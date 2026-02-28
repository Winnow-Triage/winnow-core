
import { Hero } from '../components/Hero';
import { Features } from '../components/Features';
import { HowItWorks } from '../components/HowItWorks';
import { Playground } from '../components/Playground';
import { DeepDive } from '../components/DeepDive';
import { Integration } from '../components/Integration';
import { IndependentBuilder } from '../components/IndependentBuilder';
import { CTA } from '../components/CTA';
import { SEOMeta } from '../components/SEOMeta';

export function Landing() {
    return (
        <main>
            <SEOMeta />
            <Hero />
            <Features />
            <DeepDive />
            <Integration />
            <HowItWorks />
            <Playground />
            <IndependentBuilder />
            <CTA />
        </main>
    );
}
