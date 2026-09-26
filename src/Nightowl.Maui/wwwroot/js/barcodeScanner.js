window.nightowlScanner = {
    stream: null,
    detector: null,
    isScanning: false,
    torchActive: false,
    currentZoom: 1.0,
    cropCanvas: null,
    cropCtx: null,
    lastRefocusTime: 0,

    playBeep() {
        try {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) return;
            const ctx = new AudioContext();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(987.77, ctx.currentTime); // B5 note - crisp confirmation chime
            gain.setValueAtTime(0.2, ctx.currentTime);
            gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.12);

            osc.connect(gain);
            gain.connect(ctx.destination);

            osc.start();
            osc.stop(ctx.currentTime + 0.12);
        } catch (e) {
            console.debug('Audio chirp note:', e);
        }
    },

    triggerHaptic() {
        try {
            if (navigator.vibrate) {
                navigator.vibrate([60, 40, 60]);
            }
        } catch (e) {
            console.debug('Haptic feedback note:', e);
        }
    },

    async isTorchAvailable() {
        if (!this.stream) return false;
        try {
            const track = this.stream.getVideoTracks()[0];
            if (!track || !track.getCapabilities) return false;
            const capabilities = track.getCapabilities();
            return !!capabilities.torch;
        } catch (e) {
            return false;
        }
    },

    async toggleTorch(enabled) {
        if (!this.stream) return false;
        try {
            const track = this.stream.getVideoTracks()[0];
            if (!track || !track.applyConstraints) return false;
            await track.applyConstraints({
                advanced: [{ torch: enabled }]
            });
            this.torchActive = enabled;
            return true;
        } catch (e) {
            console.warn('Torch toggle error:', e);
            return false;
        }
    },

    async getZoomCapabilities() {
        if (!this.stream) return null;
        try {
            const track = this.stream.getVideoTracks()[0];
            if (!track || !track.getCapabilities) return null;
            const caps = track.getCapabilities();
            if (caps.zoom) {
                return {
                    supported: true,
                    min: caps.zoom.min || 1.0,
                    max: caps.zoom.max || 5.0,
                    current: this.currentZoom
                };
            }
        } catch (e) {}
        return null;
    },

    async setZoom(level) {
        if (!this.stream) return false;
        try {
            const track = this.stream.getVideoTracks()[0];
            if (!track || !track.applyConstraints) return false;
            const caps = track.getCapabilities ? track.getCapabilities() : {};
            if (caps.zoom) {
                const target = Math.min(caps.zoom.max || 5.0, Math.max(caps.zoom.min || 1.0, level));
                await track.applyConstraints({
                    advanced: [{ zoom: target }]
                });
                this.currentZoom = target;
                return true;
            }
        } catch (e) {
            console.warn('Zoom failed:', e);
        }
        return false;
    },

    async triggerRefocus() {
        if (!this.stream) return false;
        try {
            const track = this.stream.getVideoTracks()[0];
            if (!track || !track.applyConstraints) return false;
            const caps = track.getCapabilities ? track.getCapabilities() : {};
            
            // Re-apply continuous autofocus and auto-exposure to force camera to refocus on subject
            const advanced = [];
            if (caps.focusMode && caps.focusMode.includes('continuous')) {
                advanced.push({ focusMode: 'continuous' });
            }
            if (caps.exposureMode && caps.exposureMode.includes('continuous')) {
                advanced.push({ exposureMode: 'continuous' });
            }
            if (advanced.length > 0) {
                await track.applyConstraints({ advanced });
                return true;
            }
        } catch (e) {
            console.debug('Refocus note:', e);
        }
        return false;
    },

    async startScanner(videoElement, dotNetHelper) {
        try {
            this.isScanning = true;
            this.torchActive = false;
            this.currentZoom = 1.0;
            this.lastRefocusTime = Date.now();

            if (!this.cropCanvas) {
                this.cropCanvas = document.createElement('canvas');
                this.cropCtx = this.cropCanvas.getContext('2d', { willReadFrequently: true });
            }

            // Initialize native BarcodeDetector if available
            if ('BarcodeDetector' in window) {
                try {
                    this.detector = new BarcodeDetector({
                        formats: ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39', 'qr_code']
                    });
                } catch (e) {
                    this.detector = new BarcodeDetector();
                }
            }

            // Higher resolution request: 1080p gives 2x sharpness on small barcodes compared to 720p
            const constraints = {
                video: {
                    facingMode: { ideal: 'environment' },
                    width: { ideal: 1920, min: 1280 },
                    height: { ideal: 1080, min: 720 }
                },
                audio: false
            };

            this.stream = await navigator.mediaDevices.getUserMedia(constraints);
            videoElement.srcObject = this.stream;
            videoElement.setAttribute('playsinline', 'true');
            await videoElement.play();

            // Apply initial autofocus and exposure constraints
            const track = this.stream.getVideoTracks()[0];
            if (track && track.getCapabilities) {
                const caps = track.getCapabilities();
                const initialAdv = [];
                if (caps.focusMode && caps.focusMode.includes('continuous')) {
                    initialAdv.push({ focusMode: 'continuous' });
                }
                if (caps.exposureMode && caps.exposureMode.includes('continuous')) {
                    initialAdv.push({ exposureMode: 'continuous' });
                }
                if (initialAdv.length > 0) {
                    try {
                        await track.applyConstraints({ advanced: initialAdv });
                    } catch (err) {}
                }
            }

            // Frame scanning loop
            const scanFrame = async () => {
                if (!this.isScanning) return;

                const now = Date.now();
                // Periodic autofocus pulse every 2.5s if not detected yet
                if (now - this.lastRefocusTime > 2500) {
                    this.lastRefocusTime = now;
                    this.triggerRefocus();
                }

                if (videoElement.readyState === videoElement.HAVE_ENOUGH_DATA && this.detector) {
                    try {
                        // 1. Try detecting on the full video frame
                        let barcodes = await this.detector.detect(videoElement);
                        
                        // 2. If nothing detected and video is large, detect on the center 50% crop (2x magnification for small barcodes!)
                        if ((!barcodes || barcodes.length === 0) && this.cropCtx && videoElement.videoWidth > 400) {
                            const vw = videoElement.videoWidth;
                            const vh = videoElement.videoHeight;
                            const cropW = Math.floor(vw * 0.55);
                            const cropH = Math.floor(vh * 0.55);
                            const cropX = Math.floor((vw - cropW) / 2);
                            const cropY = Math.floor((vh - cropH) / 2);

                            this.cropCanvas.width = cropW;
                            this.cropCanvas.height = cropH;
                            this.cropCtx.drawImage(videoElement, cropX, cropY, cropW, cropH, 0, 0, cropW, cropH);

                            barcodes = await this.detector.detect(this.cropCanvas);
                        }

                        if (barcodes && barcodes.length > 0) {
                            const code = barcodes[0].rawValue;
                            if (code && code.trim().length >= 8) {
                                this.playBeep();
                                this.triggerHaptic();

                                this.stopScanner();
                                await dotNetHelper.invokeMethodAsync('OnBarcodeDetected', code.trim());
                                return;
                            }
                        }
                    } catch (err) {
                        // Frame skip
                    }
                }

                if (this.isScanning) {
                    requestAnimationFrame(scanFrame);
                }
            };

            requestAnimationFrame(scanFrame);
            return true;
        } catch (err) {
            console.error('Failed to start camera scanner:', err);
            return false;
        }
    },

    stopScanner() {
        this.isScanning = false;
        if (this.stream) {
            this.stream.getTracks().forEach(track => {
                try { track.stop(); } catch (e) {}
            });
            this.stream = null;
        }
        this.torchActive = false;
        this.currentZoom = 1.0;
    }
};
