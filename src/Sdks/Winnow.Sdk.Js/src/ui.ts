import { getContext } from './recorder';
import { captureSafeScreenshot } from './screenshot';

export interface WinnowConfig {
    apiKey: string;
    apiUrl: string;
    tenantId?: string;
    debug?: boolean;
    onBeforeSend?: (reportPayload: any) => any | null;
}

export function initUI(config: WinnowConfig) {
    const shadowHost = document.createElement('div');
    shadowHost.id = 'winnow-sdk-host';
    document.body.appendChild(shadowHost);

    const shadow = shadowHost.attachShadow({ mode: 'open' });

    // Inject Styles
    const style = document.createElement('style');
    style.textContent = `
        .winnow-fab {
            position: fixed;
            bottom: 20px;
            right: 20px;
            width: 56px;
            height: 56px;
            border-radius: 50%;
            background-color: #3b82f6;
            color: white;
            border: none;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 9999;
            transition: transform 0.2s;
        }
        .winnow-fab:hover {
            transform: scale(1.05);
            background-color: #2563eb;
        }
        .winnow-modal-overlay {
            position: fixed;
            top: 0;
            left: 0;
            width: 100vw;
            height: 100vh;
            background: rgba(0, 0, 0, 0.5);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 10000;
            opacity: 0;
            pointer-events: none;
            transition: opacity 0.2s;
        }
        .winnow-modal-overlay.open {
            opacity: 1;
            pointer-events: auto;
        }
        .winnow-modal {
            background: white;
            padding: 24px;
            border-radius: 12px;
            width: 560px;
            max-width: 90vw;
            max-height: 85vh;
            overflow-y: auto;
            box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
            font-family: system-ui, -apple-system, sans-serif;
            color: #1f2937;
        }
        .winnow-title {
            font-size: 1.25rem;
            font-weight: 600;
            margin-bottom: 1rem;
        }
        .winnow-form-group {
            margin-bottom: 1rem;
        }
        .winnow-label {
            display: block;
            font-size: 0.875rem;
            font-weight: 500;
            margin-bottom: 0.5rem;
        }
        .winnow-input, .winnow-textarea {
            width: 100%;
            padding: 0.5rem;
            border: 1px solid #d1d5db;
            border-radius: 0.375rem;
            font-size: 0.875rem;
            box-sizing: border-box; 
        }
        .winnow-textarea {
            min-height: 100px;
            resize: vertical;
        }
        .winnow-actions {
            display: flex;
            justify-content: flex-end;
            gap: 0.5rem;
            margin-top: 1.5rem;
        }
        .winnow-btn {
            padding: 0.5rem 1rem;
            border-radius: 0.375rem;
            font-size: 0.875rem;
            font-weight: 500;
            cursor: pointer;
            border: none;
        }
        .winnow-btn-secondary {
            background: #f3f4f6;
            color: #374151;
        }
        .winnow-btn-primary {
            background: #3b82f6;
            color: white;
        }
        .winnow-btn-primary:disabled {
            background: #93c5fd;
            cursor: not-allowed;
        }
        .winnow-spinner {
            border: 2px solid #f3f3f3;
            border-top: 2px solid #3b82f6;
            border-radius: 50%;
            width: 16px;
            height: 16px;
            animation: spin 1s linear infinite;
        }
        @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

        /* Screenshot Preview */
        .winnow-screenshot-section {
            margin-bottom: 1rem;
        }
        .winnow-screenshot-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 0.5rem;
        }
        .winnow-screenshot-preview {
            border: 2px solid #e5e7eb;
            border-radius: 0.5rem;
            overflow: hidden;
            position: relative;
            background: #f9fafb;
        }
        .winnow-screenshot-preview img {
            width: 100%;
            height: auto;
            display: block;
            max-height: 250px;
            object-fit: contain;
        }
        .winnow-screenshot-preview.removed {
            opacity: 0.3;
            pointer-events: none;
        }
        .winnow-screenshot-preview img {
            cursor: zoom-in;
        }
        .winnow-screenshot-badge {
            position: absolute;
            top: 8px;
            left: 8px;
            background: rgba(0, 0, 0, 0.7);
            color: white;
            font-size: 0.7rem;
            padding: 2px 8px;
            border-radius: 4px;
            font-weight: 500;
            cursor: help;
        }
        .winnow-screenshot-badge:hover::after {
            content: 'Form inputs, passwords, and elements marked .winnow-sensitive have been automatically redacted from this screenshot.';
            position: absolute;
            top: 100%;
            left: 0;
            margin-top: 6px;
            background: #1f2937;
            color: white;
            font-size: 0.7rem;
            padding: 8px 10px;
            border-radius: 6px;
            width: 220px;
            line-height: 1.4;
            z-index: 10;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            pointer-events: none;
        }
        .winnow-lightbox {
            position: fixed;
            top: 0;
            left: 0;
            width: 100vw;
            height: 100vh;
            background: rgba(0, 0, 0, 0.85);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 10001;
            cursor: zoom-out;
            animation: winnow-fade-in 0.15s ease;
        }
        .winnow-lightbox img {
            max-width: 90vw;
            max-height: 90vh;
            object-fit: contain;
            border-radius: 8px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.5);
        }
        @keyframes winnow-fade-in { from { opacity: 0; } to { opacity: 1; } }
        .winnow-screenshot-actions {
            display: flex;
            gap: 0.5rem;
            margin-top: 0.5rem;
        }
        .winnow-screenshot-btn {
            padding: 0.25rem 0.75rem;
            border-radius: 0.375rem;
            font-size: 0.75rem;
            font-weight: 500;
            cursor: pointer;
            border: 1px solid #d1d5db;
            background: white;
            color: #374151;
            display: flex;
            align-items: center;
            gap: 0.35rem;
        }
        .winnow-screenshot-btn:hover {
            background: #f3f4f6;
        }
        .winnow-screenshot-btn.active {
            background: #fef2f2;
            border-color: #fca5a5;
            color: #dc2626;
        }
        .winnow-screenshot-loading {
            padding: 2rem;
            text-align: center;
            color: #6b7280;
            font-size: 0.875rem;
        }
    `;
    shadow.appendChild(style);

    // FAB Button
    const fab = document.createElement('button');
    fab.className = 'winnow-fab';
    fab.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path></svg>`;
    shadow.appendChild(fab);

    // Modal
    const overlay = document.createElement('div');
    overlay.className = 'winnow-modal-overlay';
    overlay.innerHTML = `
        <div class="winnow-modal">
            <div id="winnow-view-form">
                <div class="winnow-title">Report an Issue</div>
                <form id="winnow-form">
                    <div class="winnow-form-group">
                        <label class="winnow-label">Title</label>
                        <input type="text" name="title" class="winnow-input" placeholder="What went wrong?" required />
                    </div>
                    <div class="winnow-form-group">
                        <label class="winnow-label">Description</label>
                        <textarea name="description" class="winnow-textarea" placeholder="Steps to reproduce..." required></textarea>
                    </div>
                    <div class="winnow-screenshot-section" id="winnow-screenshot-section">
                        <div class="winnow-screenshot-header">
                            <span class="winnow-label" style="margin-bottom: 0;">Screenshot Preview</span>
                        </div>
                        <div id="winnow-screenshot-container">
                            <div class="winnow-screenshot-loading" id="winnow-screenshot-loading">
                                <div class="winnow-spinner" style="margin: 0 auto 0.5rem;"></div>
                                Capturing screenshot...
                            </div>
                            <div class="winnow-screenshot-preview" id="winnow-screenshot-preview" style="display: none;">
                                <span class="winnow-screenshot-badge">Privacy-masked</span>
                                <img id="winnow-screenshot-img" alt="Page screenshot (inputs redacted)" />
                            </div>
                        </div>
                        <div class="winnow-screenshot-actions">
                            <button type="button" class="winnow-screenshot-btn" id="winnow-toggle-screenshot">
                                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/></svg>
                                Remove Screenshot
                            </button>
                            <label class="winnow-screenshot-btn" id="winnow-replace-screenshot-label">
                                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
                                Replace Image
                                <input type="file" accept="image/*" id="winnow-replace-input" style="display: none;" />
                            </label>
                        </div>
                    </div>
                    <div class="winnow-actions">
                        <button type="button" class="winnow-btn winnow-btn-secondary" id="winnow-cancel">Cancel</button>
                        <button type="submit" class="winnow-btn winnow-btn-primary" id="winnow-submit">
                            <span id="winnow-submit-text">Submit Report</span>
                        </button>
                    </div>
                </form>
            </div>
            <div id="winnow-view-success" style="display: none; text-align: center;">
                <div class="winnow-title">Thank You!</div>
                <div style="margin-bottom: 1.5rem; color: #4b5563;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="#22c55e" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-bottom: 1rem; display: block; margin-left: auto; margin-right: auto;"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
                    <p>Your feedback has been submitted successfully.</p>
                </div>
                <div class="winnow-actions" style="justify-content: center;">
                    <button type="button" class="winnow-btn winnow-btn-primary" id="winnow-close-success">Close</button>
                </div>
            </div>
        </div>
    `;
    shadow.appendChild(overlay);

    // Query elements from shadow DOM
    const viewForm = overlay.querySelector('#winnow-view-form') as HTMLDivElement;
    const viewSuccess = overlay.querySelector('#winnow-view-success') as HTMLDivElement;
    const form = overlay.querySelector('#winnow-form') as HTMLFormElement;
    const screenshotSection = overlay.querySelector('#winnow-screenshot-section') as HTMLDivElement;
    const screenshotLoading = overlay.querySelector('#winnow-screenshot-loading') as HTMLDivElement;
    const screenshotPreview = overlay.querySelector('#winnow-screenshot-preview') as HTMLDivElement;
    const screenshotImg = overlay.querySelector('#winnow-screenshot-img') as HTMLImageElement;
    const toggleScreenshotBtn = overlay.querySelector('#winnow-toggle-screenshot') as HTMLButtonElement;
    const replaceInput = overlay.querySelector('#winnow-replace-input') as HTMLInputElement;

    // State
    let currentScreenshot: string | null = null;
    let screenshotIncluded = true;

    const resetView = () => {
        viewForm.style.display = 'block';
        viewSuccess.style.display = 'none';
        form.reset();
        currentScreenshot = null;
        screenshotIncluded = true;
        toggleScreenshotBtn.classList.remove('active');
        toggleScreenshotBtn.innerHTML = `
            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/></svg>
            Remove Screenshot`;
        screenshotPreview.classList.remove('removed');
        screenshotPreview.style.display = 'none';
        screenshotLoading.style.display = 'block';
    };

    // FAB click: capture screenshot first, then open modal
    fab.addEventListener('click', async () => {
        resetView();


        // Start capture
        try {
            const dataUrl = await captureSafeScreenshot();
            currentScreenshot = dataUrl;

            if (dataUrl) {
                screenshotImg.src = dataUrl;
                screenshotLoading.style.display = 'none';
                screenshotPreview.style.display = 'block';
            } else {
                screenshotLoading.innerHTML = '<span style="color: #9ca3af;">Screenshot unavailable</span>';
                screenshotSection.style.display = 'none';
            }
        } catch {
            screenshotLoading.innerHTML = '<span style="color: #9ca3af;">Screenshot unavailable</span>';
            screenshotSection.style.display = 'none';
        }
        overlay.classList.add('open');
    });

    // Toggle screenshot inclusion
    toggleScreenshotBtn.addEventListener('click', () => {
        screenshotIncluded = !screenshotIncluded;
        if (screenshotIncluded) {
            toggleScreenshotBtn.classList.remove('active');
            toggleScreenshotBtn.innerHTML = `
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/></svg>
                Remove Screenshot`;
            screenshotPreview.classList.remove('removed');
        } else {
            toggleScreenshotBtn.classList.add('active');
            toggleScreenshotBtn.innerHTML = `
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 9l6 6m0-6l-6 6"/></svg>
                Screenshot Removed`;
            screenshotPreview.classList.add('removed');
        }
    });

    // Replace image via file upload
    replaceInput.addEventListener('change', () => {
        const file = replaceInput.files?.[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = () => {
            currentScreenshot = reader.result as string;
            screenshotImg.src = currentScreenshot;
            screenshotIncluded = true;
            screenshotPreview.classList.remove('removed');
            toggleScreenshotBtn.classList.remove('active');
            toggleScreenshotBtn.innerHTML = `
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/></svg>
                Remove Screenshot`;
            // Update badge
            const badge = screenshotPreview.querySelector('.winnow-screenshot-badge') as HTMLSpanElement;
            if (badge) badge.textContent = 'User-provided';
        };
        reader.readAsDataURL(file);
    });

    // Click-to-expand lightbox
    screenshotImg.addEventListener('click', () => {
        if (!currentScreenshot || !screenshotIncluded) return;
        const lightbox = document.createElement('div');
        lightbox.className = 'winnow-lightbox';
        lightbox.innerHTML = `<img src="${currentScreenshot}" alt="Screenshot (expanded)" />`;
        lightbox.addEventListener('click', () => lightbox.remove());
        shadow.appendChild(lightbox);
    });

    // Cancel
    const cancelBtn = overlay.querySelector('#winnow-cancel') as HTMLButtonElement;
    cancelBtn.addEventListener('click', () => {
        overlay.classList.remove('open');
    });

    // Close success
    const closeSuccessBtn = overlay.querySelector('#winnow-close-success') as HTMLButtonElement;
    closeSuccessBtn.addEventListener('click', () => {
        overlay.classList.remove('open');
    });

    // Submit
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitBtn = form.querySelector('#winnow-submit') as HTMLButtonElement;
        const originalText = submitBtn.innerHTML;

        submitBtn.disabled = true;
        submitBtn.innerHTML = '<div class="winnow-spinner"></div>';

        const formData = new FormData(form);
        const title = formData.get('title') as string;
        const description = formData.get('description') as string;

        const context = getContext();

        const payload: Record<string, unknown> = {
            Title: title,
            Message: description,
            Metadata: {
                ...context,
                sdkVersion: '0.1.0'
            }
        };

        // Include screenshot if available and not removed
        if (screenshotIncluded && currentScreenshot) {
            payload.Screenshot = currentScreenshot;
        }

        let finalPayload: any = payload;

        if (typeof config.onBeforeSend === 'function') {
            const mutated = config.onBeforeSend(finalPayload);
            if (mutated === null || mutated === undefined) {
                if (config.debug) {
                    console.log('Winnow SDK: Report dropped by onBeforeSend hook.');
                }
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
                overlay.classList.remove('open');
                return;
            }
            finalPayload = mutated;
        }

        if (config.debug) {
            console.group('Winnow SDK: Sending Report');
            console.log(JSON.stringify(finalPayload, null, 2));
            console.groupEnd();
        }

        try {
            const headers: Record<string, string> = {
                'Content-Type': 'application/json',
                'X-Winnow-Key': config.apiKey
            };

            if (config.tenantId) {
                headers['X-Tenant-Id'] = config.tenantId;
            }

            const response = await fetch(`${config.apiUrl.replace(/\/$/, '')}/reports`, {
                method: 'POST',
                headers,
                body: JSON.stringify(finalPayload)
            });

            if (!response.ok) throw new Error('Failed to submit');

            // Show success view
            viewForm.style.display = 'none';
            viewSuccess.style.display = 'block';
        } catch (error) {
            console.error('Winnow SDK Error:', error);
            alert('Failed to send report. Please try again.');
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
        }
    });

    // Close on click outside
    overlay.addEventListener('click', (e) => {
        if (e.target === overlay) {
            overlay.classList.remove('open');
        }
    });
}
