// Custom lightweight frontmatter parser to avoid Node.js 'Buffer' dependency
function parseFrontmatter(text: string) {
    // Regex to match frontmatter between --- markers, handling different line endings
    const regex = /^---\s*[\r\n]+([\s\S]*?)[\r\n]+---\s*[\r\n]+([\s\S]*)$/;
    const match = text.trimStart().match(regex);

    if (!match) return { data: {}, content: text };

    const yamlBlock = match[1];
    const content = match[2].trim();
    const data: Record<string, string> = {};

    yamlBlock.split(/[\r\n]+/).forEach(line => {
        const colonIndex = line.indexOf(':');
        if (colonIndex !== -1) {
            const key = line.slice(0, colonIndex).trim();
            const value = line.slice(colonIndex + 1).trim().replace(/^["']|["']$/g, '');
            if (key) {
                data[key] = value;
            }
        }
    });

    return { data, content };
}

export interface BlogPost {
    slug: string;
    title: string;
    excerpt: string;
    category: string;
    date: string;
    readTime: string;
    image: string;
    content: string;
    author?: string;
    authorName?: string;
    authorTitle?: string;
    authorAvatar?: string;
}

// Support browser environment where 'fs' is not available
// We'll use a registry approach for simplicity in this demo, 
// or Vite glob imports if we want to be more dynamic.

const postFiles = import.meta.glob('./posts/*.md', { query: '?raw', eager: true });

function calculateReadTime(content: string): string {
    const wordsPerMinute = 200;
    const words = content.trim().split(/\s+/).length;
    const minutes = Math.max(1, Math.ceil(words / wordsPerMinute));
    return `${minutes} min read`;
}

export const blogPosts: BlogPost[] = Object.entries(postFiles).map(([path, module]: [string, any]) => {
    const slug = path.split('/').pop()?.replace('.md', '') || '';
    const { data, content } = parseFrontmatter(module.default);

    return {
        slug,
        title: data.title || 'Untitled',
        excerpt: data.excerpt || data.meta_description || '',
        category: data.category || 'Engineering',
        date: data.date || '',
        readTime: calculateReadTime(content),
        image: data.image || data.og_image || '',
        author: data.author_name || data.author || 'Winnow Team',
        authorName: data.author_name || '',
        authorTitle: data.author_title || '',
        authorAvatar: data.author_avatar || '',
        content
    };
}).sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

export function getPostBySlug(slug: string): BlogPost | undefined {
    return blogPosts.find(post => post.slug === slug);
}

export function getRecentPosts(limit = 3): BlogPost[] {
    return blogPosts.slice(0, limit);
}
