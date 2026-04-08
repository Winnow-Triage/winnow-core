import { toPng } from 'html-to-image';

const SCREENSHOT_TIMEOUT_MS = 5000;

/**
 * Captures a privacy-masked screenshot of the CURRENT VIEWPORT.
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
    // 1. Ghost Fix: Wait for animations to settle
    await new Promise(resolve => setTimeout(resolve, 200));

    // 2. Viewport Calibration
    const width = window.innerWidth;
    const height = window.innerHeight;
    const bgColor = window.getComputedStyle(document.body).backgroundColor || '#ffffff';

    // 3. The Portal Fix: Clone the document element
    // This allows us to modify values (scrub inputs) without affecting live UI
    const rootClone = document.documentElement.cloneNode(true) as HTMLElement;
    
    // Inject privacy masking styles directly into the clone
    const style = document.createElement('style');
    style.innerHTML = `
        * { 
            transition: none !important; 
            animation: none !important; 
            scroll-behavior: auto !important; 
        }
        
        .winnow-mask, [data-winnow-mask="true"], .winnow-sensitive {
            color: #0f172a !important;
            background-color: #0f172a !important;
            border-color: #0f172a !important;
            border-radius: 0.25rem !important;
            box-shadow: none !important;
            background-image: none !important;
            user-select: none !important;
        }
    `;
    rootClone.querySelector('head')?.appendChild(style);

    // Scrub values in the clone
    scrubSensitiveData(rootClone);

    // Prepare container for the clone (invisible but in DOM for style inheritance)
    const container = document.createElement('div');
    container.style.position = 'absolute';
    container.style.top = '0';
    container.style.left = '0';
    container.style.width = `${width}px`;
    container.style.height = `${height}px`;
    container.style.overflow = 'hidden';
    container.style.zIndex = '-9999';
    container.style.pointerEvents = 'none';
    container.appendChild(rootClone);
    document.body.appendChild(container);

    // Align clone to current scroll position (This ensures the viewport match)
    rootClone.style.transform = `translate(${-window.scrollX}px, ${-window.scrollY}px)`;
    rootClone.style.transformOrigin = 'top left';

    const options = {
        width: width,
        height: height,
        style: {
            margin: '0',
            padding: '0'
        },
        skipFonts: true,
        fontEmbedCSS: '',
        backgroundColor: bgColor,
        pixelRatio: window.devicePixelRatio || 2, // High quality
    };

    try {
        const dataUrl = await toPng(rootClone, options);
        return dataUrl;
    } finally {
        document.body.removeChild(container);
    }
}

function scrubSensitiveData(root: HTMLElement): void {
    // 1. Scrub all form inputs
    const inputs = root.querySelectorAll<HTMLInputElement>('input');
    inputs.forEach(el => {
        // Standard inputs
        if (['password', 'text', 'email', 'tel', 'search', 'url', 'number'].includes(el.type)) {
            el.setAttribute('value', '***');
            el.value = '***';
        }
    });

    // 2. Scrub textareas
    const textareas = root.querySelectorAll<HTMLTextAreaElement>('textarea');
    textareas.forEach(el => {
        el.innerText = '***';
        el.value = '***';
    });

    // 3. Reset selects
    const selects = root.querySelectorAll<HTMLSelectElement>('select');
    selects.forEach(el => {
        el.selectedIndex = 0;
    });

    // 4. Handle .winnow-sensitive elements
    const sensitiveEls = root.querySelectorAll<HTMLElement>('.winnow-sensitive');
    sensitiveEls.forEach(el => {
        if (el.children.length === 0) {
            el.innerText = '[REDACTED]';
        } else {
            el.innerHTML = '<div style="background:#0f172a;color:#fff;padding:8px;border-radius:4px;font-size:12px;text-align:center;">[HIDDEN]</div>';
        }
    });
}

function timeout(ms: number): Promise<null> {
    return new Promise((_, reject) =>
        setTimeout(() => reject(new Error('Screenshot timeout')), ms)
    );
}
