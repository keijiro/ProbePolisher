ProbePolisher
=============

**ProbePolisher** is a Unity Editor plugin for editing light probes. It works
both on Unity Basic (free) and Unity Pro.

Rationale
---------

Light probes are useful for filling gaps between the static lighting and
the dynamic lighting. However it is needed to place a lot of light probes
at proper positions.

Meanwhile, light probes are also useful for making ambient lights. It can
represent several environmental factors (e.g. skylights, reflections from the
ground, etc.) unlike the conventional fixed-intensity/fixed-color light.
Even more, it is possible to combine with the image-based lighting method
to make a complex lighting environment.

To make ambient lights, it needs only a few probes which is suffice to cover
the entire scene. And combined with some tricks, it is possible to modify
the lights after baking.

**ProbePolisher** is an editor plugin designed for these tasks.

Polishable probe
----------------

ProbePolisher creates a set of light probes which supports editing after
baking. For convenience, we call it *polishable probes* in this document.

Baking polishable probes (Pro only)
-----------------------------------

**Note** - *Although you need Unity Pro to bake polishable probes, you can
use pre-baked probes on Unity Basic (some pre-baked probes are included in
the package).*

1. Set up a scene for baking as you like.
2. Create a *baking jig* from "Create" -> "Baking Jig".
3. Bake the scene on the Lightmapping window.

**Tips** - You should select the *Single Lightmaps* mode before baking
to get a proper result.

Then a Light Probes asset will be created in the directory which has the same
name as the scene. You can copy/duplicate the asset to use it in another scene,
or export the asset for another project.

Using polishable probes
-----------------------

First, you have to set the Light Probes asset in the Lightmapping window.

![Setting A Light Probe]
(http://keijiro.github.io/ProbePolisher/setting-a-light-probe.png)

Then enable the 'Use Light Probes' option in renderers.

![Enabling The Light Probes]
(http://keijiro.github.io/ProbePolisher/enabling-the-light-probes.png)

**Tips** - To get better results, it is recommended to enable *Linear Lighting*
in the Player Settings.

You can edit polishable probes on the inspector.

![Inspector](http://keijiro.github.io/ProbePolisher/inspector.png)

There are two factors on the inspector -- the *base ambient* and the *skylight*.
The *base ambient* is the lighting information which is baked in the light
probe. You can adjust the influence from the base ambient with the 
*Base Intensity* value.

The *skylight* is additional lighting to the light probe. It represents a kind
of an area light source made from two hemispheres (the upper hemisphere
and the lower hemisphere).

With using these options you can modify the color of the ambient light without
re-baking the light probe.
