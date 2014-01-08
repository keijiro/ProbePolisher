ProbePolisher
=============

**ProbePolisher** is a Unity Editor plugin for editing light probes.It works
both on Unity Basic (free) and Unity Pro.

Rationale
---------

Light probes are useful for filling a gap between the static lighting
(lightmapping) and the dynamic lighting. However it's troublesome to place
light probes at proper positions.

Meanwhile, light probes are also useful for making ambient lighting. It can
represent several environmental factors (e.g. skylights, reflections from the
ground, etc.) unlike the commonly used fixed-intensity/color lighting.
Even more, there is a possibility to use it within a third-party IBL
(image based lighting) solutions like [Marmoset SkyShop]
(http://www.marmoset.co/skyshop).

In the latter case, it is needed to place just a few light probes that covers
the entire scene. And combined with some tweaks, it is possible to edit the
light probes after baking.

**ProbePolisher** is a editor plugin designed for these tasks.

Polishable probe
----------------

ProbePolisher creates a set of light probes which is suited for editing after
baking. For convenience we call it *polishable probes* in this document.

Baking polishable probes (Pro only)
-----------------------------------

*For baking polishable probes you need Unity Pro. However you can use pre-baked
probes even on Unity Basic (some pre-baked probes are included in the package).*

1. Set up the scene for baking as you like.
2. Create a *baking jig* from "Create" -> "Baking Rig".
3. Bake the scene on the Lightmapping window.

**Tips** - You should select the *single lightmapping* mode before baking
to get a proper result.

Then a Light Probes asset will be created in the directory which has the same
name as the scene. You can copy/duplicate the asset to use it in another scene,
or export the asset for another project.

Using polishable probes
-----------------------

First, you have to set the Light Probe asset in the Lightmapping window.

![Setting Light Probe]()

Then enable the 'Use Light Probes' option in renderers.

![Enable Light Probes]()

**Tips** - To get better results, it is recommended to enable Linear lighting
in the Player Settings.

You can edit polishable probes on the inspector.

![Inspector]()

There are two factors on the inspector -- the *base ambient* and the *skylight*.
The *base ambient* means the lighting information which is baked in the light
probe. The amount of the influence from the base ambient is adjusted with the
*Base Intensity* value.

The *skylight* is additional lighting for the light probe. It represents
a kind of an area light source made from two hemispheres (the upper hemisphere
and the lower hemisphere).

With using these options you can modify the color of the lighting without
re-baking light probes.
