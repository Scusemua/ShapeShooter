using UnityEngine;
using System.Collections;

public class Room : MonoBehaviour {

    // Array of all the enemies in the room.
    public BaseEnemy[] enemies;

    // States for the room.
    public enum ROOM_STATE { COMPLETE, INCOMPLETE};
    public ROOM_STATE state = ROOM_STATE.COMPLETE;

    // References to walls which are placed in the absence of doors.
    private GameObject horizontalWall;
    private GameObject verticalWall;
    // References to doors which are placed in the appropriate places based on the Doors property.
    private GameObject horizontalDoor;
    private GameObject verticalDoor;
    // References to each door object.
    private GameObject northDoor, eastDoor, westDoor, southDoor;

    // The various enemies.
    private GameObject BasicEnemy;
    private GameObject SpeedEnemy;
    private GameObject TankEnemy;

    // Coordinates of the room in the world. 
    public float x, y;
    // Coordinates of the room in the matrix.
    public int xMatrix, yMatrix;

    // ID number of the room.
    public int roomID;

    // Used for debugging the game (this is displayed in the inspector).
    public byte doorsDebug; 

    // Reference to the Floor GameObject.
    private Floor floor;

    // References to possible connected rooms.
    public Room North, South, East, West;

    // How often the room should check if all enemies are dead.
    private float checkRate = 2.0f;
    private float timeUntilCheck;

    // Used to prevent the checking of enemies until they've actually been generated.
    private bool enemiesGenerated = false;

    /// <summary>
    /// A room becomes active once the player enters it. If the player isn't in the room, 
    /// then the room isn't active and enemies from that room won't chase the player.
    /// </summary>
    private bool roomIsActive = false;

    private Player player;

    // Use this for initialization
    void Start()
    {
        floor = GameObject.FindGameObjectWithTag("Floor").GetComponent<Floor>();
        timeUntilCheck = checkRate;
    }

    void Awake()
    {
        BasicEnemy = Resources.Load("Prefabs/Entities/BasicEnemy") as GameObject;
        TankEnemy = Resources.Load("Prefabs/Entities/TankEnemy") as GameObject;
        SpeedEnemy = Resources.Load("Prefabs/Entities/SpeedEnemy") as GameObject;
    }

    void FixedUpdate()
    {   
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        }


        timeUntilCheck -= Time.deltaTime;
        if (timeUntilCheck < 0)
        {
            ClearRoomIfApplicable();
            timeUntilCheck = checkRate;
        }
    }

    /// <summary>
    /// Returns the associated entry in the floor's room matrix (which holds onto the doors as well as a bit indicating whether or not the given room is special).
    /// 
    /// The zero'th bit indicates whether or not there exists a door on the west wall.
    /// The first bit indicates whether or not there exists a door on the east wall. 
    /// The second bit indicates whether or not there exists a door on the south wall.
    /// The third bit indicates whether or not there exists a door on the north wall.
    /// The fourth bit indicates whether or not the room is a special room.
    /// </summary>
    public byte Doors
    {
        get
        {
            if (floor == null)
            {
                floor = GameObject.FindGameObjectWithTag("Floor").GetComponent<Floor>();
            }
            return floor.RoomsMatrix[xMatrix, yMatrix];
        }
        set
        {
            if (floor == null)
            {
                floor = GameObject.FindGameObjectWithTag("Floor").GetComponent<Floor>();
            }
            floor.SetMatrixEntry(xMatrix, yMatrix, value);
        }
    }

    /// <summary>
    /// Destroys all the enemy GameObjects and removes the doors if all the enemies are dead.
    /// 
    /// Returns true if all enemies in this room are dead, otherwise returns false.
    /// </summary>
    public bool ClearRoomIfApplicable()
    {
        if (state == ROOM_STATE.COMPLETE) return true;
        // Iterate through the array of enemies checking if each enemy is alive. If any enemy is alive, immediately return false.
        foreach (BaseEnemy b in enemies)
        {
            if (b.IsAlive()) return false;
        }
        // Room Clear!
        Debug.Log("Room " + roomID + " cleared successfully!");
        // Destroy all the game objects now that we don't care about their states.
        foreach (BaseEnemy e in enemies)
        {
            Destroy(e);
        }
        // Remove all the non-null doors.
        if (northDoor != null) Destroy(northDoor);
        if (southDoor != null) Destroy(southDoor);
        if (westDoor != null) Destroy(westDoor);
        if (eastDoor != null) Destroy(eastDoor);

        // Update the room state.
        state = ROOM_STATE.COMPLETE;
        return true;
    }

    /// <summary>
    /// Function to be called once the doors array has been set up from outside the class.
    /// This function uses the doors array to place walls in the appropriate places.
    /// </summary>
    public void PlaceWalls()
    {
        horizontalWall = Resources.Load("Prefabs/Floor/HorizontalWall") as GameObject;
        verticalWall = Resources.Load("Prefabs/Floor/VerticalWall") as GameObject;
        // Again, the order of the bool in the doors array is North, South, East, West.
        if ((Doors & (1 << 3)) == 0) // North
        {
            GameObject wall = Instantiate(horizontalWall, this.transform.position + new Vector3(0, 8, 0), this.transform.rotation) as GameObject;
            GUIText t = wall.AddComponent<GUIText>();
            t.text = "This was added by Room " + roomID;
        }

        if ((Doors & (1 << 2)) == 0) // South
        {
            GameObject wall = Instantiate(horizontalWall, this.transform.position + new Vector3(0, -8, 0), this.transform.rotation) as GameObject;
            GUIText t = wall.AddComponent<GUIText>();
            t.text = "This was added by Room " + roomID;
        }

        if ((Doors & (1 << 1)) == 0) // East
        {
            GameObject wall = Instantiate(verticalWall, this.transform.position + new Vector3(8, 0, 0), this.transform.rotation) as GameObject;
            GUIText t = wall.AddComponent<GUIText>();
            t.text = "This was added by Room " + roomID;
        }

        if ((Doors & 1) == 0) // West
        {
            GameObject wall = Instantiate(verticalWall, this.transform.position + new Vector3(-8, 0, 0), this.transform.rotation) as GameObject;
            GUIText t = wall.AddComponent<GUIText>();
            t.text = "This was added by Room " + roomID;
        }
    }

    /// <summary>
    /// Function to be called once the doors array has been set up from outside the class.
    /// This function uses the doors array to place doors in the appropriate places.
    /// </summary>
    public void PlaceDoors()
    {
        horizontalDoor = Resources.Load("Prefabs/Floor/HorizontalDoor") as GameObject;
        verticalDoor = Resources.Load("Prefabs/Floor/VerticalDoor") as GameObject;
        // Again, the order of the bool in the doors array is North, South, East, West.
        // Note that we check if the neighboring room already has a door generated in what is effectively the same spot. If it does,
        // we do not generate another door so as to avoid generating two doors in the same spot.
        if ((Doors & (1 << 3)) == 1) // North
        {
            if (North.southDoor != null) northDoor = North.southDoor;
            else
            {
                GameObject doorGO = Instantiate(horizontalDoor, this.transform.position + new Vector3(0, 8, 0), this.transform.rotation) as GameObject;
                northDoor = doorGO;
            }
        }

        if ((Doors & (1 << 2)) == 1) // South
        {
            // If the room to the South of this room already has a North door, just use that door to avoid generating two doors on the same position.
            if (South.northDoor != null) southDoor = South.northDoor;
            else 
            {
                GameObject doorGO = Instantiate(horizontalDoor, this.transform.position + new Vector3(0, -8, 0), this.transform.rotation) as GameObject;
                southDoor = doorGO;
            }
        }

        if ((Doors & (1 << 1)) == 1) // East
        {
            if (East.westDoor != null) eastDoor = East.westDoor;
            else
            {
                GameObject doorGO = Instantiate(verticalDoor, this.transform.position + new Vector3(8, 0, 0), this.transform.rotation) as GameObject;
                eastDoor = doorGO;
            }
        }

        if ((Doors & 1) == 1) // West
        {
            if (West.eastDoor != null) westDoor = West.eastDoor;
            else
            {
                GameObject doorGO = Instantiate(verticalDoor, this.transform.position + new Vector3(-8, 0, 0), this.transform.rotation) as GameObject;
                westDoor = doorGO;  
            }
        }
    }

    /// <summary>
    /// Returns true if the player is in this room, otherwise returns false.
    /// </summary>
    /// <returns></returns>
    public bool IsPlayerInThisRoom()
    {
        Vector3 playerPosition = player.transform.position;
        double halfSizeX = floor.RoomSize.x / 2.0;
        double halfSizeY = floor.RoomSize.y / 2.0;
        // TODO: This could be heavily optimized to only check rooms near player (instead of every room).
        if (playerPosition.x > transform.position.x - halfSizeX && playerPosition.x < transform.position.x + halfSizeX
            && playerPosition.y > transform.position.y - halfSizeY && playerPosition.y < transform.position.y + halfSizeY)
        {
            roomIsActive = true;
            return true;
        }
        else
        {
            // if (roomIsActive == true) print("PLAYER LEFT ROOM " + roomID);
            roomIsActive = false;
            return false;
        }
    }

    /// <summary>
    /// Generates the enemies for the room.
    /// </summary>
    public void GenerateEnemies()
    {
        // Calculate the number of enemies to generate per room.
        int numEnemies = 17 + floor.CurrentFloor;
        int numFast = Random.Range(0, (numEnemies / 3) - 2);
        // remainingEnemies -= numFast;
        int numTank = Random.Range(0, (numEnemies / 3) - 1);
        // remainingEnemies -= numTank;
        int numBasic = Random.Range(0, (numEnemies / 3));
        // remainingEnemies -= numBasic;

        int totalEnemies = numBasic + numTank + numFast;

        if (totalEnemies > 0) state = ROOM_STATE.INCOMPLETE;

        enemies = new BaseEnemy[totalEnemies];
        int index = 0;

        // Bounds for the placement of an enemy.
        float minX = ((-1 * floor.RoomSize.x) / 2) + 2;
        float maxX = (floor.RoomSize.x / 2) - 2;
        float minY = ((-1 * floor.RoomSize.y) / 2) + 2;
        float maxY = (floor.RoomSize.y / 2) - 2;

        for (int i = 0; i < numFast; i++)
        {
            float x = Random.Range(minX, maxX) + this.x;
            float y = Random.Range(minY, maxY) + this.y;
            enemies[index] = Instantiate(SpeedEnemy, new Vector3(x, y, 0), Quaternion.identity).GetComponent<BaseEnemy>();
            enemies[index].Room = this.GetComponent<Room>();
            index++;
        }
        for (int j = 0; j < numTank; j++)
        {
            float x = Random.Range(minX, maxX) + this.x;
            float y = Random.Range(minY, maxY) + this.y;
            enemies[index] = Instantiate(TankEnemy, new Vector3(x, y, 0), Quaternion.identity).GetComponent<BaseEnemy>();
            enemies[index].Room = this.GetComponent<Room>();
            index++;
        }
        for (int k = 0; k < numBasic; k++)
        {
            float x = Random.Range(minX, maxX) + this.x;
            float y = Random.Range(minY, maxY) + this.y;
            enemies[index] = Instantiate(BasicEnemy, new Vector3(x, y, 0), Quaternion.identity).GetComponent<BaseEnemy>();
            enemies[index].Room = this.GetComponent<Room>();
            index++;
        }

        enemiesGenerated = true;
    }

    /// <summary>
    /// Returns true if the room is active (player is in the room). Otherwise, returns false.
    /// </summary>
    public bool IsActive()
    {
        return roomIsActive; 
    }

    /// <summary>
    /// Returns the key which is used to store the room in the roomsHash.
    /// </summary>
    public string GenerateRoomHash()
    {
        return "(" + xMatrix + "," + yMatrix + ")";
    }

    public void PrintNeighboringRoomIDs()
    {
        string str = "Current Room: " + roomID + " -- ";
        if (North != null) str += "North: " + North.roomID;
        else str += "North: N/A";
        if (South != null) str += ", South: " + South.roomID;
        else str += ", South: N/A";
        if (East != null) str += ", East: " + East.roomID;
        else str += ", East: N/A";
        if (West != null) str += ", West: " + West.roomID;
        else str += ", West: N/A";

        print(str);
    }
}
