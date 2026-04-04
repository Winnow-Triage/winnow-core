import React, { useState } from 'react';
import { useWinnow } from './WinnowContext';
import { ReportPayload } from '../core';

interface FeedbackFormProps {
    onSuccess?: () => void;
    onError?: (error: Error) => void;
    initialTitle?: string;
    initialDescription?: string;
}

export const FeedbackForm: React.FC<FeedbackFormProps> = ({ 
    onSuccess, 
    onError, 
    initialTitle = '',
    initialDescription = '' 
}) => {
    const client = useWinnow();
    const [title, setTitle] = useState(initialTitle);
    const [description, setDescription] = useState(initialDescription);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitted, setSubmitted] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);

        const payload: ReportPayload = {
            Title: title,
            Message: description,
            Metadata: {
                sdk: 'winnow-react',
                version: '0.2.0'
            }
        };

        try {
            await client.sendReport(payload);
            setSubmitted(true);
            onSuccess?.();
        } catch (err: any) {
            onError?.(err);
            alert('Failed to submit report. Please try again.');
        } finally {
            setIsSubmitting(false);
        }
    };

    if (submitted) {
        return (
            <div style={styles.successContainer}>
                <h2 style={styles.title}>Thank You!</h2>
                <p>Your report has been submitted successfully.</p>
                <button 
                    onClick={() => setSubmitted(false)} 
                    style={styles.buttonPrimary}
                >
                    Send Another
                </button>
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit} style={styles.form}>
            <h2 style={styles.title}>Report an Issue</h2>
            <div style={styles.formGroup}>
                <label style={styles.label}>Title</label>
                <input 
                    type="text" 
                    value={title} 
                    onChange={(e) => setTitle(e.target.value)} 
                    style={styles.input}
                    placeholder="Brief summary"
                    required
                />
            </div>
            <div style={styles.formGroup}>
                <label style={styles.label}>Description</label>
                <textarea 
                    value={description} 
                    onChange={(e) => setDescription(e.target.value)} 
                    style={styles.textarea}
                    placeholder="What happened? How can we reproduce it?"
                    required
                />
            </div>
            <button 
                type="submit" 
                disabled={isSubmitting} 
                style={isSubmitting ? styles.buttonDisabled : styles.buttonPrimary}
            >
                {isSubmitting ? 'Submitting...' : 'Submit Report'}
            </button>
        </form>
    );
};

const styles: Record<string, React.CSSProperties> = {
    form: {
        display: 'flex',
        flexDirection: 'column',
        gap: '1rem',
        padding: '1.5rem',
        backgroundColor: '#fff',
        borderRadius: '12px',
        boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
        fontFamily: 'system-ui, -apple-system, sans-serif'
    },
    successContainer: {
        textAlign: 'center',
        padding: '2rem',
        backgroundColor: '#fff',
        borderRadius: '12px',
        fontFamily: 'system-ui, -apple-system, sans-serif'
    },
    title: {
        fontSize: '1.25rem',
        fontWeight: 600,
        margin: '0 0 0.5rem 0'
    },
    formGroup: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.25rem'
    },
    label: {
        fontSize: '0.875rem',
        fontWeight: 500,
        color: '#374151'
    },
    input: {
        padding: '0.5rem',
        border: '1px solid #d1d5db',
        borderRadius: '6px',
        fontSize: '0.875rem'
    },
    textarea: {
        padding: '0.5rem',
        border: '1px solid #d1d5db',
        borderRadius: '6px',
        fontSize: '0.875rem',
        minHeight: '100px',
        resize: 'vertical'
    },
    buttonPrimary: {
        padding: '0.625rem',
        backgroundColor: '#3b82f6',
        color: '#fff',
        border: 'none',
        borderRadius: '6px',
        fontWeight: 500,
        cursor: 'pointer'
    },
    buttonDisabled: {
        padding: '0.625rem',
        backgroundColor: '#93c5fd',
        color: '#fff',
        border: 'none',
        borderRadius: '6px',
        fontWeight: 500,
        cursor: 'not-allowed'
    }
};
