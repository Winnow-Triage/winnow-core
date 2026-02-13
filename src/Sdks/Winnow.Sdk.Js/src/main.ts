import { startRecording } from './recorder';
import { initUI, WinnowConfig } from './ui';

declare global {
    interface Window {
        Winnow: {
            init: (config: WinnowConfig) => void;
        };
    }
}

const Winnow = {
    init: (config: WinnowConfig) => {
        if (!config.apiKey || !config.apiUrl) {
            console.error('Winnow SDK: apiKey and apiUrl are required.');
            return;
        }

        console.log('Winnow SDK Initializing...');
        startRecording();
        initUI(config);
    }
};

window.Winnow = Winnow;

export default Winnow;
