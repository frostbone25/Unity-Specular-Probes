# Unity Specular Probes
A simple editor script that places an emissive object at the place of every light in the scene to emulate specular highlights on reflection probes when baking them.

![window](GithubContent/window.png)

## Results

Specular Probes Enabled
![result1-on](GithubContent/result1-on.png)

Specular Probes Disabled
![result1-off](GithubContent/result1-off.png)

Specular Probes Enabled
![result2-on](GithubContent/result2-on.png)

Specular Probes Disabled
![result2-off](GithubContent/result2-off.png)

### Why?
The unity standard shader does not have a specular highlight term when baking lightmaps *(even with directional lightmaps, but it only shades the diffuse term)*. As a result when a light source is baked fully within unity, you lose out on the specular highlight term you would normally get from realtime/mixed lights *(unless you are using the shader that computes a specular highlight using the directional term from the lightmap)*. For PBR rendering, having a good specular term is critical for getting good and correct shading results.

This is a script that attempts to alleviate that by spawning an emissive object *(that matches the shape of the light source)* before reflection probes are baked in an attempt to emulate the specular lobe of the light. All of this at no extra performance cost at all! *(because it's being baked directly into the reflection probes)*.

### Credits
I was inspired by [Zulubo's](https://github.com/zulubo/SpecularProbes) implementation, however I opted for my own and went for a more simplified but global solution that also can do area lights as well.
