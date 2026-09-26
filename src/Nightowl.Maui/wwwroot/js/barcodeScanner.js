window.nightowlScanner = {
    stream: null,
    detector: null,
    isScanning: false,
    torchActive: false,

    playBeep() {
        try {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) return;
            const ctx = new AudioContext();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(987.77, ctx.currentTime); // B5 note - cheerful clear chime
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
                // Short, crisp double-pulse haptic vibration
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

    async startScanner(videoElement, dotNetHelper) {
        try {
            this.isScanning = true;
            this.torchActive = false;

            // Initialize native BarcodeDetector if available in Chrome/Android WebView
            if ('BarcodeDetector' in window) {
                try {
                    this.detector = new BarcodeDetector({
                        formats: ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39', 'qr_code']
                    });
                } catch (e) {
                    this.detector = new BarcodeDetector();
                }
            }

            // High-priority environment (rear) camera with autofocus
            const constraints = {
                video: {
                    facingMode: { ideal: 'environment' },
                    width: { ideal: 1280 },
                    height: { ideal: 720 },
                    focusMode: { ideal: 'continuous' }
                },
                audio: false
            };

            this.stream = await navigator.mediaDevices.getUserMedia(constraints);
            videoElement.srcObject = this.stream;
            videoElement.setAttribute('playsinline', 'true');
            await videoElement.play();

            // Continuous frame scanning loop
            const scanFrame = async () => {
                if (!this.isScanning) return;

                if (videoElement.readyState === videoElement.HAVE_ENOUGH_DATA) {
                    if (this.detector) {
                        try {
                            const barcodes = await this.detector.detect(videoElement);
                            if (barcodes && barcodes.length > 0) {
                                const code = barcodes[0].rawValue;
                                if (code && code.trim().length >= 8) {
                                    // Feedback
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
    }
};
