window.nightowlScanner = {
    stream: null,
    detector: null,
    isScanning: false,

    async startScanner(videoElement, dotNetHelper) {
        try {
            this.isScanning = true;

            // Initialize BarcodeDetector if available
            if ('BarcodeDetector' in window) {
                try {
                    this.detector = new BarcodeDetector({
                        formats: ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39', 'qr_code']
                    });
                } catch (e) {
                    console.warn('BarcodeDetector format config fallback:', e);
                    this.detector = new BarcodeDetector();
                }
            }

            // Request camera stream with back camera priority
            const constraints = {
                video: {
                    facingMode: { ideal: 'environment' },
                    width: { ideal: 1280 },
                    height: { ideal: 720 }
                },
                audio: false
            };

            this.stream = await navigator.mediaDevices.getUserMedia(constraints);
            videoElement.srcObject = this.stream;
            await videoElement.play();

            // Frame scanning loop
            const scanFrame = async () => {
                if (!this.isScanning) return;

                if (videoElement.readyState === videoElement.HAVE_ENOUGH_DATA) {
                    if (this.detector) {
                        try {
                            const barcodes = await this.detector.detect(videoElement);
                            if (barcodes && barcodes.length > 0) {
                                const code = barcodes[0].rawValue;
                                if (code && code.trim().length >= 8) {
                                    this.stopScanner();
                                    await dotNetHelper.invokeMethodAsync('OnBarcodeDetected', code.trim());
                                    return;
                                }
                            }
                        } catch (err) {
                            // Frame detection skip
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
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
        }
    }
};
