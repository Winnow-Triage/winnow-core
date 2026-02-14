
import { Hero } from '../components/Hero';
import { Features } from '../components/Features';
import { HowItWorks } from '../components/HowItWorks';
import { Playground } from '../components/Playground';
import { DeepDive } from '../components/DeepDive';
import { Integration } from '../components/Integration';

export function Landing() {
    return (
        <main>
            <Hero />
            <Features />
            <DeepDive />
            <Integration />
            <HowItWorks />
            <Playground />
        </main>
    );
}
