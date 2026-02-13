import { getContext } from './recorder';

export interface WinnowConfig {
    apiKey: string;
    apiUrl: string;
    tenantId?: string;
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
            border-radius: 8px;
            width: 400px;
            max-width: 90vw;
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
                <div class="winnow-title">Report a Bug</div>
                <form id="winnow-form">
                    <div class="winnow-form-group">
                        <label class="winnow-label">Title</label>
                        <input type="text" name="title" class="winnow-input" placeholder="What went wrong?" required />
                    </div>
                    <div class="winnow-form-group">
                        <label class="winnow-label">Description</label>
                        <textarea name="description" class="winnow-textarea" placeholder="Steps to reproduce..." required></textarea>
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

    // Helpers to toggle views
    const viewForm = overlay.querySelector('#winnow-view-form') as HTMLDivElement;
    const viewSuccess = overlay.querySelector('#winnow-view-success') as HTMLDivElement;
    const form = overlay.querySelector('#winnow-form') as HTMLFormElement;

    const resetView = () => {
        viewForm.style.display = 'block';
        viewSuccess.style.display = 'none';
        form.reset();
    };

    // Event Listeners
    fab.addEventListener('click', () => {
        resetView();
        overlay.classList.add('open');
    });

    const cancelBtn = overlay.querySelector('#winnow-cancel') as HTMLButtonElement;
    cancelBtn.addEventListener('click', () => {
        overlay.classList.remove('open');
    });

    const closeSuccessBtn = overlay.querySelector('#winnow-close-success') as HTMLButtonElement;
    closeSuccessBtn.addEventListener('click', () => {
        overlay.classList.remove('open');
    });

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

        const payload = {
            Title: title,
            Description: description,
            Metadata: {
                ...context,
                sdkVersion: '0.0.1'
            }
        };

        try {
            const headers: Record<string, string> = {
                'Content-Type': 'application/json',
                'X-Winnow-Key': config.apiKey
            };

            if (config.tenantId) {
                headers['X-Tenant-Id'] = config.tenantId;
            }

            const response = await fetch(`${config.apiUrl.replace(/\/$/, '')}/tickets`, {
                method: 'POST',
                headers,
                body: JSON.stringify(payload)
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
