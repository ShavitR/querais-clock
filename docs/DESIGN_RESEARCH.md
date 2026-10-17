# The detailed collection

Research and implementation, 17 October 2026.

The aim of this pass is a much more convincing desktop object from every angle. More triangles alone do not establish better design. These are original Querais models; no reference manufacturer's geometry, photographs or logos are included.

## What the references changed

| Primary reference | Observation | Querais decision |
| --- | --- | --- |
| [Bang & Olufsen: aluminium craftsmanship](https://www.bang-olufsen.com/en/nl/story/all-about-aluminium) | Brushing, polishing, anodising and material joints create distinct surfaces and controlled reflections. | Hollow extruded frames, separate polished aperture edges, satin bodies, shadow joints, soft isolation pads and microscopic tooling roughness. A generic rounded box cannot substitute for a case section. |
| [MB&F / L'Epée: Starfleet Machine](https://www.mbandf.com/mbf-co-creations/lepee-1839/starfleet-machine) | The visible clockwork and its supporting structure are part of the object. | Atelier has a rear exhibition movement with pierced tooth wheels, open bridges, ruby bearings, a balance rim and a six-turn hairspring. Relay has individual cartridges, radial card stacks, axles and indexing wheels. |
| [Ressence: oil-filled optics](https://ressencewatches.com/pages/oil-filled) | Optical decisions serve legibility: the brand describes reducing apparent separation between the dial and crystal. | Keep glare restrained, separate the glass edge from its face, and preserve clear digits and hands. Querais does not simulate an oil-filled movement or claim equivalent optical performance. |
| [Solari Lineadesign: custom Cifra 3](https://www.solarilineadesign.com/en/creative-spirit-and-perfection-of-the-made-in-italy-label-the-customized-projects-of-solari-lineadesign--en.html) | A mechanically readable flip object can have a strong identity through disciplined casing and print. | Relay's readable plates remain the visual centre. Repetition belongs in the card edges, hinges and cassette construction rather than decoration across the numbers. |

These are design inferences from the cited manufacturers, not endorsements or reproductions.

## Four distinct constructions

- **Atelier:** stepped case, circumferential joint, domed box crystal, compression gasket, socket fasteners, signed crown and an exhibition back. The front remains a quiet instrument. The reverse rewards orbiting.
- **Slate:** a deep hollow aluminium extrusion around a thin floating display, diamond-cut aperture, knurled side controls and separate cantilever feet. Inspection exposes a board, controller packages, leads, solder pads and copper paths beneath a removable service panel.
- **Relay:** an open structural cage around four independent flap cartridges, retaining rails, axles, bushings, radial card stacks and brass indexing wheels. Existing live minute transitions still use independent plates at their own seam pivots.
- **Signal:** extruded segment emitters sit above individual recessed optical wells. The enclosure adds a polished optical bevel, top cooling ribs, rear fins and an inspectable electronic assembly.

Every digital object has a modeled rear service cover, recessed connector well, tongue and eight contacts. The mechanisms and electronics are illustrative visual assemblies, not manufacturing or circuit specifications. The app reads system time; decorative clockwork does not calculate it.

## Runtime honesty and budgets

Blender Cycles renders show actual geometry under area lights with metal, roughness, transmission and microscopic bump materials. They are previews, never the clock image used in the app.

The app remains native WPF. Its fixed-function renderer does not implement Cycles or full physically based rendering. A normal-driven studio lookup approximates environment reflections, with UV coordinates refreshed as the view, parts and light move. This replaces the old object-wide painted metal gradient. Reflection work is capped at 10 updates per second and static batches are skipped when their orientation is unchanged. The actual geometry, hands, flaps, segments, finish selection and inspection transforms remain interactive.

Authoring bounds: fewer than 350,000 triangles and 120 material batches per visible object. Following [Microsoft's WPF 3D performance guidance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance), native batches stay below 18,000 positions and 54,000 indices, static data is frozen, and brushes/materials are reused. The dense modeled dial lettering is replaced by a native overlay. Typed streaming avoids a large JSON token tree, and the scene cache retains at most two objects.

Current counts are recorded by `tools/validate_assets.py`. Hidden/minimized suspension and reduced motion are checked in the native verification pass. Native screenshots revealed that emissive segments needed an opaque diffuse depth layer to remain visible against the added optical wells. The resulting combined material follows [Microsoft's diffuse/emissive material example](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-apply-emissive-material-to-a-3-d-object). No numerical “10x quality” claim is made; judge the Blender and native captures together.
