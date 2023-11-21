using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class MapLogic : MonoBehaviour
{
    public enum Terrain { desert, drylands, grass, snow, ocean, sea, mountains, badlands, forest, hills, jungle, reef, lava, snowForest, snowMountains, snowWater, swamp, volcano, beach };
    public enum Direction { up, down, upleft, upright, downleft, downright };

    //Dictionary cells, the individual hexes, the key Vector3Int is the location coordinates in the hex map
    public Dictionary<Vector3Int, MapCell> cellList = new Dictionary<Vector3Int, MapCell>();

    //The hex tile map
    public Tilemap hexTileMap;

    //These tilemaps and rule tiles handle drawing the borders, the borders are layered to allow for 6 sprites to handle all combinations for a hex map
    public List<Tilemap> borderTilemaps = new List<Tilemap>();
    public List<HexagonalRuleTile> borderTiles2 = new List<HexagonalRuleTile>();

    //Tile map borders
    public int minXPos = -16;
    public int maxXPos = 17;
    public int minYPos = -8;
    public int maxYPos = 9;

    public GameObject mapDetailsParentObj;

    //The wave function collapse rules for individual cells inside a provice, for instance in a snow biome can contain snow, snow forest, grass, icy water, snow mountains and sea tiles, with different probabilities of bordering each other
    public List<WaveFunctionTile> waveFunctionTiles = new List<WaveFunctionTile>();
    //The wave function collapse rules for provinces, a minimal and maximum tempature (colder biomes near the top, hotter near the bottom), and a list of tiles each can contain
    public List<WaveFunctionProvince> waveFunctionProvinces = new List<WaveFunctionProvince>();
    //Default province if no others work
    public WaveFunctionProvince grasslands;

    public int amountOfProvinces = 20;
    public GameObject provObj;
    public GameObject provObjParent;
    //Generated provinces
    public List<Province> provinces = new List<Province>();
    public List<HexagonalRuleTile> borderTiles = new List<HexagonalRuleTile>();

    public List<NameList> provinceNameLists = new List<NameList>();

    public GameLogic gameLogic;

    List<GameObject> generatedObjects = new List<GameObject>();

    private Vector3Int midPoint = new Vector3Int(0,0,0);
    private int tenPercentX = 50;
    private int tenPercentY = 50;

    public IntRange amountOfCities;
    public GameObject cityObj;

    public List<GameObject> listOfCities = new List<GameObject>();
    public int cityRoadBuildingIndex = 0;

    //Grouping of terrains for different functionalities
    public List<Terrain> allTerrains = new List<Terrain>();
    public List<Terrain> groundTerrains = new List<Terrain>();
    public List<Terrain> waterTerrains = new List<Terrain>();
    public List<Terrain> difficultTerrains = new List<Terrain>();
    public List<Terrain> mountainTerrains = new List<Terrain>();

    public TileBase roadTile;

    public NameList guildNameOptions;
    public NameList cityNameOptions;

    void Start() {
        midPoint = new Vector3Int((maxXPos / 2), (maxYPos / 2), 0);
        tenPercentX = maxXPos / 10;
        tenPercentY = maxYPos / 10;
    }

    //Start of the map gen process
    //DrawHexMap is called from MapLogicInspectorHex, allowing this to be done in the editor but can be called as part of the a world generation
    public void DrawHexMap() {
        DrawHexMapProvinces();
    }

    public void DrawHexMapProvinces() {
        //Clear existing data
        DestroyAllChildrenImmediate(provObjParent);
        provinces = new List<Province>();
        cellList = new Dictionary<Vector3Int, MapCell>();

        Dictionary<Vector3Int, MapCell> tempCellList = new Dictionary<Vector3Int, MapCell>();
        for (int col = minYPos; col < maxYPos; col++) {
            for (int row = minXPos; row < maxXPos; row++) {
                Vector3Int position = new Vector3Int(row, col, 0);
                MapCell cell = new MapCell();
                cell.cellPosition = groundTileMap.CellToLocal(position);
                cell.cellIntPosition = position;
                tempCellList.Add(position, cell);
            }
        }

        float tempChange = (float)100f / maxXPos;

        //Creating each province
        for (int i = 0; i < amountOfProvinces; i++) {
            //Create the province obj to store the data in game
            GameObject newProvObj = Instantiate(provObj, provObjParent.transform);
            Province p = newProvObj.GetComponent<Province>();

            //Picks a random spot on the tile map as the provinces starting point and first cell
            bool foundStartingPos = false;
            Vector3Int startingPos = new Vector3Int(0, 0, 0);
            while (!foundStartingPos) {
                startingPos = new Vector3Int(UnityEngine.Random.Range(minXPos, maxXPos), UnityEngine.Random.Range(minYPos, maxYPos), 0);
                if (tempCellList.ContainsKey(startingPos)) {
                    foundStartingPos = true;
                }
            }

            //Determine the possible terrain options for the province
            float posTemp = tempChange * startingPos.x;
            List<WaveFunctionProvince> vfpOptions = new List<WaveFunctionProvince>();
            foreach (WaveFunctionProvince wfp in waveFunctionProvinces) {
                if (posTemp >= wfp.minTempVal && posTemp <= wfp.maxTempVal) {
                    vfpOptions.Add(wfp);
                }
            }
            if (vfpOptions.Count.Equals(0)) {
                vfpOptions.Add(grasslands);
            }

            p.waveFunctionProvince = vfpOptions[UnityEngine.Random.Range(0, vfpOptions.Count)];

            //Add the starting cell to the province
            MapCell cell = tempCellList[startingPos];

            p.cellList.Add(startingPos, cell);
            p.cellList2.Add(cell);

            //Generate some other details for the province
            p.Population = UnityEngine.Random.Range(2000, 50000);
            foreach (NameList nameList in provinceNameLists) {
                if (nameList.specificTerrain.Equals(p.waveFunctionProvince.defaultTile.terrain)) {
                    bool southern = false;
                    bool northern = false;
                    if (posTemp > 75) {
                        northern = true;
                    } else if (posTemp < 25) {
                        southern = true;
                    }
                    p.ProvinceName = DetermineProvinceName(nameList, northern, southern);
                }
            }

            provinces.Add(p);
            tempCellList.Remove(cell.cellIntPosition);
        }

        //Loops through each province, each one grabs one random unassigned neighbour cell and adds it to the province
        //This continues until all cells are assigned to aprovince
        while (tempCellList.Count > 0) {
            foreach (Province p in provinces) {
                if (tempCellList.Count > 0) {

                    MapCell startingCell = p.cellList2[UnityEngine.Random.Range(0, p.cellList2.Count)];
                    List<MapCell> cellsToPickFrom = GetNeighbourCells(tempCellList, returnAllDirections(), null, startingCell.cellIntPosition);

                    if (cellsToPickFrom.Count > 0) {
                        MapCell newCell = cellsToPickFrom[UnityEngine.Random.Range(0, cellsToPickFrom.Count)];

                        p.cellList.Add(newCell.cellIntPosition, newCell);

                        tempCellList.Remove(newCell.cellIntPosition);
                        cellsToPickFrom.Remove(newCell);
                    }
                }
            }
        }

        int cellId = 0;
        //doing the terrain generation for the provinces - in order up and down, left to right so that the wavefunction terrain generation works without issue
        for (int col = minYPos; col < maxYPos; col++) {
            for (int row = minXPos; row < maxXPos; row++) {
                Province p = null;
                MapCell cell = null;

                Vector3Int gridPos = new Vector3Int(row, col, 0);
                foreach (Province prov in provinces) {
                    MapCell tempCell = null;
                    if (prov.cellList.TryGetValue(gridPos, out tempCell)) {
                        p = prov;
                        cell = tempCell;
                    }
                }

                if (p != null) {
                    //Get list of possible terrains based on the set rules
                    float posTemp = tempChange * cell.cellIntPosition.x;
                    List<WaveFunctionTile> allowableTerrains = new List<WaveFunctionTile>();
                    foreach (WaveFunctionTileJoin w2 in p.waveFunctionProvince.tileOptions) {
                        //temp check
                        if (posTemp >= w2.joinedTile.minTempVal && posTemp <= w2.joinedTile.maxTempVal) {
                            allowableTerrains.Add(w2.joinedTile);
                            for (int i = 0; i <= w2.probability; i++) {
                                allowableTerrains.Add(w2.joinedTile);
                            }
                        }
                    }

                    //now restrict allowableTerrains based on the neighbour cells terrain
                    List<MapCell> adjanctCells = GetNeighbourCells(cellList, returnAllDirections(), null, gridPos);

                    //Pick the terrain
                    List<WaveFunctionTile> newAllowableTerrains = new List<WaveFunctionTile>();
                    foreach (WaveFunctionTile wft2 in allowableTerrains) {
                        List<Terrain> terrainOptions = new List<Terrain>();
                        foreach (WaveFunctionTileJoin wftj2 in wft2.joinOptions) {
                            terrainOptions.Add(wftj2.joinedTile.terrain);
                        }

                        bool canJoin = true;
                        foreach (MapCell c2 in adjanctCells) {
                            if (!terrainOptions.Contains(c2.terrain)) {
                                canJoin = false;
                            }
                        }

                        if (canJoin) {
                            newAllowableTerrains.Add(wft2);
                        }
                    }

                    WaveFunctionTile wft = p.waveFunctionProvince.defaultTile;
                    if (newAllowableTerrains.Count > 0) {
                        wft = newAllowableTerrains[UnityEngine.Random.Range(0, newAllowableTerrains.Count)];
                    }

                    cell.terrain = wft.terrain;

                    Vector3Int startingPos = cell.cellIntPosition;
                    //DrawCell draws the sprite onto the hex map
                    DrawCell(startingPos.x, startingPos.y, cellId, wft);
                    cellId++;
                }
            }
        }

        //Create borders for each province
        foreach (Province p in provinces) {
            foreach (MapCell cell in p.cellList2) {
                SetNeighbourCellsBorders(p.cellList, cell, cell.cellIntPosition, Color.black);
            }

        }

        //Generates small details, like cities, more to be added
        GenerateMapDetails();

        groundTileMap.RefreshAllTiles();
        foreach (Tilemap tilemap in borderTilemaps) {
            tilemap.RefreshAllTiles();
        }
    }

    public void DrawCell(int x, int y, int cellId, WaveFunctionTile wft) {
        Vector3Int position = new Vector3Int(x, y, 0);

        groundTileMap.SetTile(position, wft.tile);

        MapCell cell = new MapCell();
        cell.cellId = cellId;
        cell.terrain = wft.terrain;
        cell.cellPosition = groundTileMap.CellToLocal(position);
        cell.cellIntPosition = position;
        cellList.Add(position, cell);
    }

    public MapCell CellGetAtPosition(Vector3Int targetPost) {
        MapCell c = null;
        cellList.TryGetValue(targetPost, out c);
        return c;
    }

    public MapCell CellGetAtPosition(Vector2 targetPost) {
        Vector3Int newPos = new Vector3Int((int)targetPost.x, (int)targetPost.y, 0);

        MapCell c = null;
        cellList.TryGetValue(newPos, out c);

        return c;
    }

    //ReadMap is a useful function, which can scan the tile map and re-creates the variables from that, used as are currently calling DrawHexMap from the editor
    public void ReadMap() {
        cellList = new Dictionary<Vector3Int, MapCell>();
        int cellId = 0;
        for (int col = minYPos; col < maxYPos; col++) {
            for (int row = minXPos; row < maxXPos; row++) {
                Vector3Int position = new Vector3Int(row, col, 0);
                Terrain terrain = DetermineTerrainFromGrid(position);

                TileBase t = groundTileMap.GetTile(position);
                if (t != null) {
                    MapCell cell = new MapCell();
                    cell.cellId = cellId;
                    cell.terrain = terrain;
                    cell.cellPosition = groundTileMap.CellToLocal(position);
                    cell.cellIntPosition = new Vector3Int(row, col, 0);

                    TileBase t2 = roadMap.GetTile(position);
                    if (t2 != null) {
                        if (t2.Equals(roadTile)) {
                            cell.hasRoad = true;
                        }
                    }

                    cellList.Add(cell.cellIntPosition, cell);

                    cellId++;
                }
            }
        }
    }

    public void GenerateMapDetails() {
        //delete all current map details
        DestroyAllChildrenImmediate(mapDetailsParentObj);
        generatedObjects = new List<GameObject>();
        for (int col = minYPos; col < maxYPos; col++) {
            for (int row = minXPos; row < maxXPos; row++) {
                Vector3Int position = new Vector3Int(row, col, 0);
                roadMap.SetTile(position, null);
            }
        }
        roadMap.RefreshAllTiles();

        midPoint = new Vector3Int((maxXPos / 2), (maxYPos / 2), 0);
        tenPercentX = maxXPos / 10;
        tenPercentY = maxYPos / 10;

        //cities
        listOfCities = new List<GameObject>();
        int citiesAmount = UnityEngine.Random.Range(amountOfCities.m_Min, amountOfCities.m_Max);
        for (int i = 0; i < citiesAmount; i++) {
            Province prov = null;
            MapCell cell = null;
            List<Terrain> allowTerrains = groundTerrains;
            GameObject newCity = SpawnObjectAwayFromOtherObjects(cityObj, 5f, allowTerrains, out prov, out cell);

            City city = newCity.GetComponent<City>();
            string cityName = DetermineRandomNameFromList(cityNameOptions);
            city.name = cityName;
            city.cityName = cityName;

            listOfCities.Add(newCity);
        }
    }

    public GameObject SpawnObjectAwayFromOtherObjects(GameObject toSpawn, float distanceCheck, List<Terrain> allowTerrains, out Province prov2, out MapCell cell2) {
        cell2 = null;
        prov2 = null;
        int attempts = 0;
        GameObject newGo = null;

        while (cell2 == null && attempts < 50) {
            Province prov = provinces[UnityEngine.Random.Range(0, provinces.Count)];
            MapCell testCell = prov.cellList2[UnityEngine.Random.Range(0, prov.cellList2.Count)];

            attempts++;

            if (NoOtherObjectsNearby(testCell.cellPosition, distanceCheck)) {
                if (allowTerrains == null || allowTerrains.Contains(testCell.terrain)) {
                    newGo = Instantiate(toSpawn, mapDetailsParentObj.transform);
                    newGo.transform.position = testCell.cellPosition;

                    generatedObjects.Add(newGo);

                    cell2 = testCell;
                    prov2 = prov;
                }
            }
        }

        return newGo;
    }

    public bool NoOtherObjectsNearby(Vector2 testPos, float disCheck) {
        bool tooClose = false;

        float distanceToClosestOtherHeadquarters = 200f;
        foreach (GameObject go in generatedObjects) {
            float newDist = Vector2.Distance(go.transform.position, testPos);
            if (newDist < distanceToClosestOtherHeadquarters) {
                distanceToClosestOtherHeadquarters = newDist;
            }
        }

        if (distanceToClosestOtherHeadquarters < disCheck) {
            tooClose = true;
        }

        return !tooClose;
    }

    public bool TrendTowardsCenterOfMap(Vector3Int testPos, int multiplier) {
        bool nearCenter = false;

        int xMax = midPoint.x + (tenPercentX * multiplier);
        int yMax = midPoint.y + (tenPercentY * multiplier);
        int xMin = midPoint.x - (tenPercentX * multiplier);
        int yMin = midPoint.y - (tenPercentY * multiplier);

        if (testPos.x >= xMin && testPos.y >= yMin && testPos.x <= xMax && testPos.y <= yMax) {
            nearCenter = true;
        }

        return nearCenter;
    }

    //Returns all neighbour cells for the pos
    public List<MapCell> GetNeighbourCells(Dictionary<Vector3Int, MapCell> listOfCells, List<Direction> directions, List<Terrain> terrainFilters, Vector3Int pos) {
        List<MapCell> neighbourCells = new List<MapCell>();

        MapCell newCell = null;
        if (directions.Contains(Direction.up)) {
            if (listOfCells.TryGetValue(new Vector3Int(pos.x + 1, pos.y, 0), out newCell)) {
                if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                    neighbourCells.Add(newCell);
                }
            }
        }

        if (directions.Contains(Direction.down)) {
            if (listOfCells.TryGetValue(new Vector3Int(pos.x - 1, pos.y, 0), out newCell)) {
                if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                    neighbourCells.Add(newCell);
                }
            }
        }

        if (directions.Contains(Direction.upleft)) {
            if (pos.y % 2 == 0) {
                if (listOfCells.TryGetValue(new Vector3Int(pos.x, pos.y - 1, 0), out newCell)) {
                    if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                        neighbourCells.Add(newCell);
                    }
                }
            } else {
                if (listOfCells.TryGetValue(new Vector3Int(pos.x + 1, pos.y - 1, 0), out newCell)) {
                    if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                        neighbourCells.Add(newCell);
                    }
                }
            }
        }

        if (directions.Contains(Direction.upright)) {
            if (pos.y % 2 == 0) {
                if (listOfCells.TryGetValue(new Vector3Int(pos.x, pos.y + 1, 0), out newCell)) {
                    if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                        neighbourCells.Add(newCell);
                    }
                }
            } else {
                if (listOfCells.TryGetValue(new Vector3Int(pos.x + 1, pos.y + 1, 0), out newCell)) {
                    if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                        neighbourCells.Add(newCell);
                    }
                }
            }
        }

        if (directions.Contains(Direction.downleft)) {
            if (pos.y % 2 == 0) {
                if (listOfCells.TryGetValue(new Vector3Int(pos.x - 1, pos.y - 1, 0), out newCell)) {
                    if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                        neighbourCells.Add(newCell);
                    }
                }
            } else {
                if (listOfCells.TryGetValue(new Vector3Int(pos.x, pos.y - 1, 0), out newCell)) {
                    if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                        neighbourCells.Add(newCell);
                    }
                }
            }
        }

        if (directions.Contains(Direction.downright)) {
            if (pos.y % 2 == 0) {
                if (listOfCells.TryGetValue(new Vector3Int(pos.x - 1, pos.y + 1, 0), out newCell)) {
                    if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                        neighbourCells.Add(newCell);
                    }
                }
            } else {
                if (listOfCells.TryGetValue(new Vector3Int(pos.x, pos.y + 1, 0), out newCell)) {
                    if (terrainFilters == null || terrainFilters.Contains(newCell.terrain)) {
                        neighbourCells.Add(newCell);
                    }
                }
            }
        }

        return neighbourCells;
    }

    public void SetNeighbourCellsBorders(Dictionary<Vector3Int, MapCell> listOfCells, MapCell cell, Vector3Int pos, Color color) {
        //new method, determing all borders and placing stacking border tiles for each side
        MapCell newCell = null;

        foreach (Tilemap tilemap in borderTilemaps) {
            tilemap.SetTile(cell.cellIntPosition, null);
        }

        if (!listOfCells.TryGetValue(new Vector3Int(pos.x + 1, pos.y, 0), out newCell)) {
            borderTilemaps[0].SetTile(cell.cellIntPosition, borderTiles2[0]);
        }

        if (!listOfCells.TryGetValue(new Vector3Int(pos.x - 1, pos.y, 0), out newCell)) {
            borderTilemaps[1].SetTile(cell.cellIntPosition, borderTiles2[1]);
        }

        if (pos.y % 2 == 0) {
            if (!listOfCells.TryGetValue(new Vector3Int(pos.x, pos.y - 1, 0), out newCell)) {
                borderTilemaps[2].SetTile(cell.cellIntPosition, borderTiles2[2]);
            }
        } else {
            if (!listOfCells.TryGetValue(new Vector3Int(pos.x + 1, pos.y - 1, 0), out newCell)) {
                borderTilemaps[2].SetTile(cell.cellIntPosition, borderTiles2[2]);
            }
        }

        if (pos.y % 2 == 0) {
            if (!listOfCells.TryGetValue(new Vector3Int(pos.x, pos.y + 1, 0), out newCell)) {
                borderTilemaps[3].SetTile(cell.cellIntPosition, borderTiles2[3]);
            }
        } else {
            if (!listOfCells.TryGetValue(new Vector3Int(pos.x + 1, pos.y + 1, 0), out newCell)) {
                borderTilemaps[3].SetTile(cell.cellIntPosition, borderTiles2[3]);
            }
        }

        if (pos.y % 2 == 0) {
            if (!listOfCells.TryGetValue(new Vector3Int(pos.x - 1, pos.y - 1, 0), out newCell)) {
                borderTilemaps[4].SetTile(cell.cellIntPosition, borderTiles2[4]);
            }
        } else {
            if (!listOfCells.TryGetValue(new Vector3Int(pos.x, pos.y - 1, 0), out newCell)) {
                borderTilemaps[4].SetTile(cell.cellIntPosition, borderTiles2[4]);
            }
        }

        if (pos.y % 2 == 0) {
            if (!listOfCells.TryGetValue(new Vector3Int(pos.x - 1, pos.y + 1, 0), out newCell)) {
                borderTilemaps[5].SetTile(cell.cellIntPosition, borderTiles2[5]);
            }
        } else {
            if (!listOfCells.TryGetValue(new Vector3Int(pos.x, pos.y + 1, 0), out newCell)) {
                borderTilemaps[5].SetTile(cell.cellIntPosition, borderTiles2[5]);
            }
        }

        foreach (Tilemap tilemap in borderTilemaps) {
            tilemap.SetColor(cell.cellIntPosition, color);
        }
    }

    //Attempts to determine the terrain of a specific cell by checking the tile base/sprite
    public Terrain DetermineTerrainFromGrid(Vector3Int position) {
        TileBase t = groundTileMap.GetTile(position);

        foreach (WaveFunctionTile wft in waveFunctionTiles) {
            if (t != null && t.Equals(wft.tile)) {
                return wft.terrain;
            }
        }

        return Terrain.grass;
    }

    //Helper method to destroy all children of a given gameobject
    public void DestroyAllChildrenImmediate(GameObject parent) {
        for (int i = parent.transform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(parent.transform.GetChild(i).gameObject);
        }
    }

    //Gets a random name from a given list of options
    public string DetermineRandomNameFromList(NameList list) {
        string name = "";

        List<string> possibleName = new List<string>();
        foreach (string s1 in list.names) {
            possibleName.Add(s1);
        }
        foreach (string s2 in list.prefixes) {
            string suffix = list.sufixes[UnityEngine.Random.Range(0, list.sufixes.Count)];
            possibleName.Add(s2 + suffix);
        }
        name = possibleName[UnityEngine.Random.Range(0, possibleName.Count)];

        return name;
    }

    public string DetermineProvinceName(NameList list, bool northern, bool southern) {
        string name = "";

        List<string> possibleName = new List<string>();
        foreach (string s1 in list.names) {
            possibleName.Add(s1);
        }
        foreach (string s2 in list.prefixes) {
            string suffix = list.sufixes[UnityEngine.Random.Range(0, list.sufixes.Count)];
            possibleName.Add(s2 + suffix);
        }
        if (northern) {
            string suffix = list.sufixes[UnityEngine.Random.Range(0, list.sufixes.Count)];
            possibleName.Add("Northern " + suffix);
        }
        if (southern) {
            string suffix = list.sufixes[UnityEngine.Random.Range(0, list.sufixes.Count)];
            possibleName.Add("Southern " + suffix);
        }
        name = possibleName[UnityEngine.Random.Range(0, possibleName.Count)];

        return name;
    }

    public List<Direction> returnAllDirections() {
        List<Direction> directions = new List<MapLogic.Direction>();
        directions.Add(Direction.up);
        directions.Add(Direction.upleft);
        directions.Add(Direction.upright);
        directions.Add(Direction.down);
        directions.Add(Direction.downleft);
        directions.Add(Direction.downright);

        return directions;
    }
}
