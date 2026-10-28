#ifndef GOAT_FUR_SHADING_INCLUDED
#define GOAT_FUR_SHADING_INCLUDED
half3 GoatFurColor(half3 coat, half3 originalTint, half along)
{
    // Keep each strand close to the underlying coat, with a restrained root-to-tip gradient.
    half variation = lerp(.96h, 1.04h, saturate(dot(originalTint, half3(.3h, .59h, .11h)) * 5));
    return coat * variation * lerp(.9h, 1.02h, saturate(along));
}
#endif
