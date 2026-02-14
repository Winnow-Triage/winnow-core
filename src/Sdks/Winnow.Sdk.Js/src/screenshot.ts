import html2canvas from 'html2canvas-pro';

const SCREENSHOT_TIMEOUT_MS = 5000;

/**
 * Captures a privacy-masked screenshot of the current page.
 * All form inputs are scrubbed, and elements with `.winnow-sensitive` are redacted/blurred.
 * Returns a Base64 PNG data URL, or null if capture fails or times out.
 */
export async function captureSafeScreenshot(): Promise<string | null> {
    try {
        const result = await Promise.race([
            captureWithMasking(),
            timeout(SCREENSHOT_TIMEOUT_MS)
        ]);
        return result;
    } catch (err) {
        console.warn('[Winnow SDK] Screenshot capture failed:', err);
        return null;
    }
}

async function captureWithMasking(): Promise<string> {
    // Hide the SDK host element so it doesn't appear in the screenshot
    const sdkHost = document.getElementById('winnow-sdk-host');
    if (sdkHost) sdkHost.style.display = 'none';

    try {
        const canvas = await html2canvas(document.body, {
            useCORS: true,
            logging: false,
            scale: window.devicePixelRatio > 1 ? 1.5 : 1, // Balance quality vs size
            onclone: (_doc: Document, clonedBody: HTMLElement) => {
                scrubSensitiveData(clonedBody);
            }
        });

        return canvas.toDataURL('image/png');
    } finally {
        if (sdkHost) sdkHost.style.display = '';
    }
}

function scrubSensitiveData(root: HTMLElement): void {
    // 1. Scrub all form inputs
    const inputs = root.querySelectorAll<HTMLInputElement>('input');
    inputs.forEach(el => {
        if (el.type === 'password' || el.type === 'text' || el.type === 'email' ||
            el.type === 'tel' || el.type === 'search' || el.type === 'url' || el.type === 'number') {
            el.value = '***';
        }
    });

    // 2. Scrub textareas
    const textareas = root.querySelectorAll<HTMLTextAreaElement>('textarea');
    textareas.forEach(el => {
        el.value = '***';
    });

    // 3. Reset selects to first option (hide user choice)
    const selects = root.querySelectorAll<HTMLSelectElement>('select');
    selects.forEach(el => {
        el.selectedIndex = 0;
    });

    // 4. Handle .winnow-sensitive elements
    const sensitiveEls = root.querySelectorAll<HTMLElement>('.winnow-sensitive');
    sensitiveEls.forEach(el => {
        if (el.children.length === 0) {
            // Leaf text node — redact text
            el.innerText = '[REDACTED]';
        } else {
            // Container — replace contents with solid redacted block
            // (html2canvas doesn't support CSS filter: blur)
            el.innerHTML = '<div style="background:#374151;color:#f9fafb;padding:12px;border-radius:6px;text-align:center;font-size:0.875rem;font-weight:500;">[REDACTED — SENSITIVE CONTENT]</div>';
        }
    });
}


function timeout(ms: number): Promise<null> {
    return new Promise((_resolve, reject) =>
        setTimeout(() => reject(new Error(`Screenshot timed out after ${ms}ms`)), ms)
    );
}
