#ifndef SB_INCOMING_DECAL_SHAPE
#define SB_INCOMING_DECAL_SHAPE
// Shared by the warning ring and the extruded impact wall: the footprint never changes on hit.
float decalRadius(float angle, float radius, float roughness)
{
    return radius*(1+roughness*(sin(angle*5+.7)*.6+sin(angle*11-1.3)*.4));
}
float decalCoverage(float angle, float gaps)
{
    if (gaps <= 0) return 1;
    float pattern=.5+.27*sin(angle*7+1.1)+.23*sin(angle*13-.8);
    return smoothstep(gaps,gaps+.12,pattern);
}
float decalHash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
float decalNoise(float2 p)
{
    float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(decalHash(cell),decalHash(cell+float2(1,0)),f.x),
        lerp(decalHash(cell+float2(0,1)),decalHash(cell+1),f.x),f.y);
}
#endif
