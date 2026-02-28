import { useEffect } from 'react';

interface SEOMetaProps {
    title?: string;
    description?: string;
    ogImage?: string;
    ogType?: string;
}

/**
 * Reusable SEO and Metadata component.
 * Handles document title and meta tags dynamically without external dependencies.
 */
export function SEOMeta({
    title,
    description,
    ogImage = "https://winnowtriage.com/images/social-preview.png",
    ogType = "website"
}: SEOMetaProps) {
    const defaultDesc = "Stop drowning in duplicate bug reports. AI-driven semantic triage for modern engineering teams.";
    const siteTitle = title ? `${title} | Winnow` : "Winnow | Triage at the speed of AI";
    const finalDesc = description || defaultDesc;

    useEffect(() => {
        // Update document title
        document.title = siteTitle;

        // Helper to update or create meta tags
        const updateMeta = (name: string, content: string, isProperty = false) => {
            const attr = isProperty ? 'property' : 'name';
            let el = document.querySelector(`meta[${attr}="${name}"]`);

            if (!el) {
                el = document.createElement('meta');
                el.setAttribute(attr, name);
                document.head.appendChild(el);
            }

            el.setAttribute('content', content);
        };

        // Standard Meta Tags
        updateMeta('description', finalDesc);

        // Open Graph / Facebook / Discord
        updateMeta('og:title', siteTitle, true);
        updateMeta('og:description', finalDesc, true);
        updateMeta('og:image', ogImage, true);
        updateMeta('og:type', ogType, true);

        // Twitter Previews
        updateMeta('twitter:card', 'summary_large_image');
        updateMeta('twitter:title', siteTitle);
        updateMeta('twitter:description', finalDesc);
        updateMeta('twitter:image', ogImage);

    }, [siteTitle, finalDesc, ogImage, ogType]);

    return null; // This component does not render any visible UI
}
