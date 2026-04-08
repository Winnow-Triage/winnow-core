export interface WinnowConfig {
    apiKey: string;
    apiUrl: string; // Should default to https://api.winnowtriage.com
    tenantId?: string;
    debug?: boolean;
    onBeforeSend?: (reportPayload: any) => any | null;
}

export interface ReportPayload {
    Title: string;
    Message: string;
    StackTrace?: string;
    OsVersion?: string;
    EngineVersion?: string;
    Platform?: string;
    AppVersion?: string;
    Resolution?: string;
    ScreenshotKey?: string;
    ScreenshotSize?: number;
    Metadata?: Record<string, unknown>;
}

export class WinnowClient {
    private config: WinnowConfig;

    constructor(config: WinnowConfig) {
        this.config = {
            ...config,
            apiUrl: (config.apiUrl || 'https://api.winnowtriage.com').replace(/\/$/, '')
        };
    }

    async sendReport(payload: ReportPayload, screenshotBlob?: Blob): Promise<void> {
        let finalPayload: any = { ...payload };

        // 0. Generate Proof-of-Work once for the session (bound to /reports)
        const pow = await this.generatePoW('POST', '/reports');

        // 1. Handle Screenshot Upload (Pre-signed URL flow)
        if (screenshotBlob) {
            try {
                if (this.config.debug) {
                    console.log('Winnow SDK: Starting screenshot upload flow...');
                }

                const fileName = `screenshot-${Date.now()}.png`;

                const presignHeaders: Record<string, string> = {
                    'Content-Type': 'application/json',
                    'X-Winnow-Key': this.config.apiKey,
                    'X-Winnow-PoW-Nonce': pow.nonce,
                    'X-Winnow-PoW-Timestamp': pow.timestamp
                };
                if (this.config.tenantId) {
                    presignHeaders['X-Tenant-Id'] = this.config.tenantId;
                }

                const presignRes = await fetch(`${this.config.apiUrl}/storage/upload-url`, {
                    method: 'POST',
                    headers: presignHeaders,
                    body: JSON.stringify({
                        fileName: fileName,
                        contentType: screenshotBlob.type,
                        fileSizeBytes: screenshotBlob.size
                    })
                });

                if (!presignRes.ok) {
                    throw new Error(`Failed to get pre-signed URL: ${presignRes.status}`);
                }

                const presignData = await presignRes.json();
                const uploadUrl = presignData.uploadUrl;
                const screenshotKey = presignData.objectKey;

                // Direct PUT to S3
                const uploadRes = await fetch(uploadUrl, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': screenshotBlob.type
                    },
                    body: screenshotBlob
                });

                if (!uploadRes.ok) {
                    throw new Error(`S3 upload failed: ${uploadRes.status}`);
                }

                finalPayload.ScreenshotKey = screenshotKey;
                finalPayload.ScreenshotSize = screenshotBlob.size;

            } catch (err) {
                console.warn('Winnow SDK: Screenshot upload failed, submitting report without it.', err);
                // Continue with the report even if screenshot fails
            }
        }

        // 2. Pre-send Hook
        if (typeof this.config.onBeforeSend === 'function') {
            const mutated = this.config.onBeforeSend(finalPayload);
            if (mutated === null || mutated === undefined) {
                if (this.config.debug) {
                    console.log('Winnow SDK: Report dropped by onBeforeSend hook.');
                }
                return;
            }
            finalPayload = mutated;
        }

        // 3. Submit Report with Proof-of-Work
        try {
            const headers: Record<string, string> = {
                'Content-Type': 'application/json',
                'X-Winnow-Key': this.config.apiKey,
                'X-Winnow-PoW-Nonce': pow.nonce,
                'X-Winnow-PoW-Timestamp': pow.timestamp
            };

            if (this.config.tenantId) {
                headers['X-Tenant-Id'] = this.config.tenantId;
            }

            const response = await fetch(`${this.config.apiUrl}/reports`, {
                method: 'POST',
                headers,
                body: JSON.stringify(finalPayload)
            });

            if (!response.ok) throw new Error(`Failed to submit report: ${response.status}`);
            
            if (this.config.debug) {
                console.log('Winnow SDK: Report submitted successfully.');
            }
        } catch (error) {
            console.error('Winnow SDK Error:', error);
            throw error;
        }
    }

    private async generatePoW(method: string, path: string): Promise<{ nonce: string; timestamp: string }> {
        const targetPrefix = "0000"; // Difficulty 4
        const encoder = new TextEncoder();
        
        const getUuid = () => {
            if (typeof crypto.randomUUID === 'function') {
                return crypto.randomUUID().replace(/-/g, '');
            }
            return Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
        };

        while (true) {
            const timestamp = new Date().toISOString();
            const nonce = getUuid();
            
            const data = `${this.config.apiKey || ""}${method.toUpperCase()}${path.toLowerCase()}${timestamp}${nonce}`;
            const dataBuffer = encoder.encode(data);
            const hashBuffer = await crypto.subtle.digest('SHA-256', dataBuffer);
            
            const hashArray = Array.from(new Uint8Array(hashBuffer));
            const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
            
            if (hashHex.startsWith(targetPrefix)) {
                return { nonce, timestamp };
            }
        }
    }
}
