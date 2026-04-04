import React, { createContext, useContext, useMemo } from 'react';
import { WinnowClient, WinnowConfig } from '../core';

const WinnowContext = createContext<WinnowClient | null>(null);

export const WinnowProvider: React.FC<{ config: WinnowConfig; children: React.ReactNode }> = ({ config, children }) => {
    const client = useMemo(() => new WinnowClient(config), [config]);
    return <WinnowContext.Provider value={client}>{children}</WinnowContext.Provider>;
};

export const useWinnow = () => {
    const context = useContext(WinnowContext);
    if (!context) {
        throw new Error('useWinnow must be used within a WinnowProvider');
    }
    return context;
};
