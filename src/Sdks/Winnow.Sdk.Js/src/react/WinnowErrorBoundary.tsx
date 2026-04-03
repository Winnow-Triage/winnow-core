import React, { Component, ReactNode, ErrorInfo } from 'react';
import { FeedbackForm } from './FeedbackForm';

interface Props {
    children: ReactNode;
    fallback?: ReactNode;
    showFeedbackForm?: boolean;
    onReset?: () => void;
}

interface State {
    hasError: boolean;
    error: Error | null;
}

export class WinnowErrorBoundary extends Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = { hasError: false, error: null };
    }

    static getDerivedStateFromError(error: Error): State {
        return { hasError: true, error };
    }

    componentDidCatch(error: Error, errorInfo: ErrorInfo) {
        console.error('WinnowErrorBoundary caught an error:', error, errorInfo);
        // Note: Future versions of WinnowClient could auto-send this here.
    }

    handleReset = () => {
        this.setState({ hasError: false, error: null });
        this.props.onReset?.();
    };

    render() {
        if (this.state.hasError) {
            if (this.props.fallback) {
                return this.props.fallback;
            }

            if (this.props.showFeedbackForm !== false) {
                return (
                    <div style={styles.container}>
                        <h1 style={styles.header}>Something went wrong.</h1>
                        <p style={styles.text}>A crash report has been generated. You can provide more details below to help us fix it.</p>
                        <FeedbackForm 
                            initialTitle={this.state.error?.name || 'Application Error'}
                            initialDescription={this.state.error?.message || ''}
                            onSuccess={this.handleReset}
                        />
                        <button onClick={this.handleReset} style={styles.resetBtn}>
                            Try Again
                        </button>
                    </div>
                );
            }

            return <h1>Something went wrong.</h1>;
        }

        return this.props.children;
    }
}

const styles: Record<string, React.CSSProperties> = {
    container: {
        padding: '2rem',
        maxWidth: '600px',
        margin: '2rem auto',
        fontFamily: 'system-ui, -apple-system, sans-serif'
    },
    header: {
        fontSize: '1.5rem',
        fontWeight: 700,
        color: '#111827',
        marginBottom: '1rem'
    },
    text: {
        color: '#4b5563',
        marginBottom: '2rem'
    },
    resetBtn: {
        marginTop: '1rem',
        padding: '0.5rem 1rem',
        backgroundColor: '#f3f4f6',
        color: '#374151',
        border: 'none',
        borderRadius: '6px',
        cursor: 'pointer'
    }
};
