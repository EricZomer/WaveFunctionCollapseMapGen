# WaveFunctionCollapseMapGen
A C# map generator class using wave function collapse for Unity for hex tile maps

## Overview
A map generator project for a Unity game I've been working on, it breaks the hex map into provinces with specific terrains and then uses wave function collapse to draw the specific cells.

Starts with MapLogic.DrawHexMap();

## Map Gen Techniques
I went through a few different variations before arriving at this technique:

-Perlin Noise: Had first uses perlin noise to generate the terrain. However this ended up generating maps that can be very similar and its hard to make rules that handle small difference in terrain like the difference between shallow sea and a coral sea

-Wave Function Collapse (Random): First implementation of wave function collapse, this is the standard implementation you will find online. You pick a random tile from the tile map, determine its terrain and then pick one of its neighbours and repeat. When you pick a cell you check all neighbour cells to determine what the terrain can be based on a set of rules (Ocean can only be next to Sea, Sea can be next to Beach, Beach next to Grass, etc). However because of the random nature of picking the next cell you will sometimes end up with neighbour cells that can have no possible valid choices. Like a cell that has Ocean on one side and Grass on the other, both Sea and Beach dont fit.

-Wave Function Collapse (Ordered): Second implementation of wave function collapse, this is similar to the original implementation but instead the tiles are picked from bottom right and then going up/down from right to left in order. Picking the tiles in this order makes sure that the neighbour cells are always valid.

-Wave Function Collapse (Provinces): My final implementation, and the one documented here. Went with this route as I wanted to break the map into provinces to separate out the different terrains into different regions. More details below.

## Wave Function Collapse (Provinces)
-The tile map is first broken into provinces, as its easier to first break it up and then generate the right terrain in that province than to generate the terrain and then try and fit provinces into the random borders. This way provinces are more consistent in size, though still random in shape/size.
So it picks a random tile for each province to start, and a terrain for the province.

-It then loops and for each province it grabs an unassigned neighbour tile and adds it to the set of province tiles, until there are no unassigned tiles left.

-It then goes through as described in Wave Function Collapse (Ordered), though picking from the terrains allowed from the assigned province.

-After terrain has been picked for each cell, it draws borders for each province

-Lastly it generates map details like cities and such

