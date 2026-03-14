import { toPng } from 'html-to-image';

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
    // 1. The Animation Delay (The Ghost Fix)
    // Allow CSS animations and transitions to finish before cloning
    await new Promise(resolve => setTimeout(resolve, 250));

    const width = document.documentElement.clientWidth;
    const height = window.innerHeight;
    const bgColor = window.getComputedStyle(document.body).backgroundColor || '#ffffff';

    const options = {
        width: width,
        height: height,
        canvasWidth: width,
        canvasHeight: height,
        pixelRatio: 1,
        backgroundColor: bgColor,

        // --- OPTIMIZATION & BUG FIX ---
        // Bypass web font parsing to prevent Firefox `TypeError: can't access property "trim" of undefined`
        // and avoid cross-origin CSS rule getter DOMExceptions.
        skipFonts: true,
        fontEmbedCSS: '',

        style: {
            transform: `translate(${-window.scrollX}px, ${-window.scrollY}px)`,
            transformOrigin: 'top left',
            // --- VIEWPORT ENFORCER ---
            // Use clientWidth (viewport) instead of scrollWidth (full document)
            width: `${width}px`,
            height: `${document.documentElement.scrollHeight}px`,
            margin: '0',
            padding: '0'
        },
        // 3. Refine the Exclusion Filter
        filter: (node: Node) => {
            const el = node as HTMLElement;
            // Strictly exclude only the widget container
            return el.id !== 'winnow-widget-container' && el.id !== 'winnow-sdk-host';
        }
    };

    console.log('[Winnow SDK] Capturing with Viewport Enforcer fixes:', options);

    // 2. Target the Absolute Root (The Portal Fix)
    const rootClone = document.documentElement.cloneNode(true) as HTMLElement;

    // --- THE VIEWPORT ENFORCER: Style Injection ---
    const style = document.createElement('style');
    style.innerHTML = `
      /* --- LAYOUT HEALER --- */
      .w-screen { width: 100% !important; max-width: 100% !important; }
      .min-w-screen { min-width: 100% !important; }
      .h-screen { height: 100% !important; max-height: 100% !important; }
      
      /* --- ANIMATION KILLER --- */
      * { 
        transition: none !important; 
        animation: none !important; 
        scroll-behavior: auto !important; 
      }
      
      /* --- PRIVACY MASKING --- */
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

    const head = rootClone.querySelector('head');
    if (head) {
        head.appendChild(style);
    } else {
        rootClone.appendChild(style);
    }

    // --- THE VIEWPORT ENFORCER: Root Locking ---
    // Force HTML and Body to be exactly the viewport width to satisfy media queries
    rootClone.style.width = `${width}px`;
    rootClone.style.overflowX = 'hidden';

    const clonedBody = rootClone.querySelector('body');
    if (clonedBody) {
        clonedBody.style.width = `${width}px`;
        clonedBody.style.overflowX = 'hidden';
    }

    const clonedReactRoot = rootClone.querySelector('#root') as HTMLElement;
    if (clonedReactRoot) {
        clonedReactRoot.style.width = `${width}px`;
        clonedReactRoot.style.overflowX = 'hidden';
    }

    // --- THE LAYOUT FREEZER ---
    const structuralSelectors = 'section, header, main, footer, .container, [class*="mx-auto"], [class*="max-w-"]';
    const liveElements = document.querySelectorAll(structuralSelectors);
    const clonedElements = rootClone.querySelectorAll(structuralSelectors);

    liveElements.forEach((liveEl, index) => {
        const cloneEl = clonedElements[index] as HTMLElement;
        if (cloneEl) {
            const computed = window.getComputedStyle(liveEl);
            cloneEl.style.width = computed.width;
            cloneEl.style.marginLeft = computed.marginLeft;
            cloneEl.style.marginRight = computed.marginRight;
            cloneEl.style.maxWidth = computed.maxWidth;
        }
    });

    // Force dimensions on the clone root
    rootClone.style.width = `${width}px`;
    rootClone.style.height = `${document.documentElement.scrollHeight}px`;
    rootClone.style.overflow = 'hidden';
    rootClone.style.backgroundColor = bgColor;

    scrubSensitiveData(rootClone);

    rootClone.style.position = 'fixed';
    rootClone.style.top = '0';
    rootClone.style.left = '0';
    rootClone.style.zIndex = '-9999';
    rootClone.style.pointerEvents = 'none';
    document.body.appendChild(rootClone);

    try {
        const dataUrl = await toPng(rootClone, options);
        return dataUrl;
    } finally {
        document.body.removeChild(rootClone);
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
