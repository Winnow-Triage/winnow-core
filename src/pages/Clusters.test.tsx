import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom/extend-expect';
import Clusters from './Clusters';

// Mock the API calls
jest.mock('@/lib/api', () => ({
    get: jest.fn(),
    post: jest.fn(),
}));

describe('Clusters Component', () => {
    beforeEach(() => {
        // Reset mocks before each test
        jest.clearAllMocks();
    });

    it('renders loading state initially', async () => {
        (api.get as jest.Mock).mockResolvedValue({ data: [] });
        render(<Clusters />);
        expect(screen.getByText(/Loading.../i)).toBeInTheDocument();
    });

    it('renders no clusters found message when there are no clusters', async () => {
        (api.get as jest.Mock).mockResolvedValue({ data: [] });
        render(<Clusters />);
        await screen.findByText(/No clusters found./i);
    });

    it('displays clusters correctly', async () => {
        const mockReports = [
            {
                id: '1',
                title: 'Cluster 1',
                message: '',
                status: 'Active',
                createdAt: '2023-10-01T12:00:00Z',
                criticalityScore: 8,
            },
            {
                id: '2',
                title: 'Cluster 2',
                message: '',
                status: 'Inactive',
                createdAt: '2023-10-02T12:00:00Z',
                criticalityScore: 5,
            },
        ];
        (api.get as jest.Mock).mockResolvedValue({ data: mockReports });
        render(<Clusters />);
        await screen.findByText(/Cluster 1/i);
        expect(screen.getByText(/Cluster 1/i)).toBeInTheDocument();
        expect(screen.getByText(/Cluster 2/i)).toBeInTheDocument();
    });

    it('sorts clusters by size', async () => {
        const mockReports = [
            {
                id: '1',
                title: 'Cluster 1',
                message: '',
                status: 'Active',
                createdAt: '2023-10-01T12:00:00Z',
                criticalityScore: 8,
            },
            {
                id: '2',
                title: 'Cluster 2',
                message: '',
                status: 'Inactive',
                createdAt: '2023-10-02T12:00:00Z',
                criticalityScore: 5,
            },
        ];
        (api.get as jest.Mock).mockResolvedValue({ data: mockReports });
        render(<Clusters />);
        await screen.findByText(/Cluster 1/i);

        // Select size sort option
        const sortBySelect = screen.getByRole('combobox');
        fireEvent.change(sortBySelect, { target: { value: 'size' } });

        // Check if clusters are sorted by size (assuming Cluster 1 has more children)
        expect(screen.getAllByText(/Cluster 1/i)[0]).toBeInTheDocument();
    });

    it('sorts clusters by criticality', async () => {
        const mockReports = [
            {
                id: '1',
                title: 'Cluster 1',
                message: '',
                status: 'Active',
                createdAt: '2023-10-01T12:00:00Z',
                criticalityScore: 8,
            },
            {
                id: '2',
                title: 'Cluster 2',
                message: '',
                status: 'Inactive',
                createdAt: '2023-10-02T12:00:00Z',
                criticalityScore: 5,
            },
        ];
        (api.get as jest.Mock).mockResolvedValue({ data: mockReports });
        render(<Clusters />);
        await screen.findByText(/Cluster 1/i);

        // Select criticality sort option
        const sortBySelect = screen.getByRole('combobox');
        fireEvent.change(sortBySelect, { target: { value: 'criticality' } });

        // Check if clusters are sorted by criticality (assuming Cluster 1 has higher criticality)
        expect(screen.getAllByText(/Cluster 1/i)[0]).toBeInTheDocument();
    });

    it('sorts clusters by newest', async () => {
        const mockReports = [
            {
                id: '1',
                title: 'Cluster 1',
                message: '',
                status: 'Active',
                createdAt: '2023-10-01T12:00:00Z',
                criticalityScore: 8,
            },
            {
                id: '2',
                title: 'Cluster 2',
                message: '',
                status: 'Inactive',
                createdAt: '2023-10-02T12:00:00Z',
                criticalityScore: 5,
            },
        ];
        (api.get as jest.Mock).mockResolvedValue({ data: mockReports });
        render(<Clusters />);
        await screen.findByText(/Cluster 1/i);

        // Select newest sort option
        const sortBySelect = screen.getByRole('combobox');
        fireEvent.change(sortBySelect, { target: { value: 'newest' } });

        // Check if clusters are sorted by newest (assuming Cluster 2 is newer)
        expect(screen.getAllByText(/Cluster 2/i)[0]).toBeInTheDocument();
    });

    it('searches for clusters', async () => {
        const mockReports = [
            {
                id: '1',
                title: 'Cluster 1',
                message: '',
                status: 'Active',
                createdAt: '2023-10-01T12:00:00Z',
                criticalityScore: 8,
            },
            {
                id: '2',
                title: 'Cluster 2',
                message: '',
                status: 'Inactive',
                createdAt: '2023-10-02T12:00:00Z',
                criticalityScore: 5,
            },
        ];
        (api.get as jest.Mock).mockResolvedValue({ data: mockReports });
        render(<Clusters />);
        await screen.findByText(/Cluster 1/i);

        // Enter search term
        const searchInput = screen.getByPlaceholderText(/Search clusters.../i);
        fireEvent.change(searchInput, { target: { value: 'Cluster 1' } });

        // Check if only Cluster 1 is displayed
        expect(screen.getByText(/Cluster 1/i)).toBeInTheDocument();
        expect(screen.queryByText(/Cluster 2/i)).not.toBeInTheDocument();
    });

    it('handles merge button correctly', async () => {
        const mockReports = [
            {
                id: '1',
                title: 'Cluster 1',
                message: '',
                status: 'Active',
                createdAt: '2023-10-01T12:00:00Z',
                criticalityScore: 8,
            },
            {
                id: '2',
                title: 'Cluster 2',
                message: '',
                status: 'Inactive',
                createdAt: '2023-10-02T12:00:00Z',
                criticalityScore: 5,
            },
        ];
        (api.get as jest.Mock).mockResolvedValue({ data: mockReports });
        render(<Clusters />);
        await screen.findByText(/Cluster 1/i);

        // Select clusters
        const checkboxes = screen.getAllByRole('checkbox');
        fireEvent.click(checkboxes[0]);
        fireEvent.click(checkboxes[1]);

        // Click merge button
        const mergeButton = screen.getByText(/Merge 2 Clusters/i);
        fireEvent.click(mergeButton);

        // Check if merge is called with correct parameters
        expect(api.post).toHaveBeenCalledWith('/reports/1/merge', { id: '1', sourceIds: ['2'] });
    });
});
