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

    // How often the room should check if all enemies are dead.
    private float checkRate = 2.0f;
    private float timeUntilCheck = 2.0f;

    // Used to prevent the checking of enemies until they've actually been generated.
    private bool enemiesGenerated = false;

    // Use this for initialization
    void Start()
    {
        floor = GameObject.FindGameObjectWithTag("Floor").GetComponent<Floor>();
    }

    void Awake()
    {
        BasicEnemy = Resources.Load("Prefabs/Entities/BasicEnemy") as GameObject;
        TankEnemy = Resources.Load("Prefabs/Entities/TankEnemy") as GameObject;
        SpeedEnemy = Resources.Load("Prefabs/Entities/SpeedEnemy") as GameObject;
    }

    void FixedUpdate()
    {
        // Decrement the cooldown to fire.
        timeUntilCheck -= Time.deltaTime;
        if (enemiesGenerated && timeUntilCheck < 0)
        {
            IsRoomClear();
        }
    }

    // Update is called once per frame
    void Update()
    {

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
    /// Returns true if all enemies in this room are dead, otherwise returns false.
    /// </summary>
    public bool IsRoomClear()
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
        // Update the room state.
        state = ROOM_STATE.COMPLETE;
        return true;
    }

    /// <summary>
    /// Function to be called once the doors array has been set up from outside the class.
    /// This function uses the doors array to place doors/walls in the appropriate places.
    /// </summary>
    public void PlaceDoors()
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
    /// Generates the enemies for the room.
    /// </summary>
    public void GenerateEnemies()
    {
        // Calculate the number of enemies to generate per room.
        int numEnemies = 13 + floor.CurrentFloor;
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
            enemies[index++] = Instantiate(SpeedEnemy, new Vector3(x, y, 0), Quaternion.identity).GetComponent<BaseEnemy>();
        }
        for (int j = 0; j < numTank; j++)
        {
            float x = Random.Range(minX, maxX) + this.x;
            float y = Random.Range(minY, maxY) + this.y;
            enemies[index++] = Instantiate(TankEnemy, new Vector3(x, y, 0), Quaternion.identity).GetComponent<BaseEnemy>();
        }
        for (int k = 0; k < numBasic; k++)
        {
            float x = Random.Range(minX, maxX) + this.x;
            float y = Random.Range(minY, maxY) + this.y;
            enemies[index++] = Instantiate(BasicEnemy, new Vector3(x, y, 0), Quaternion.identity).GetComponent<BaseEnemy>();
        }

        enemiesGenerated = true;
    }

    /// <summary>
    /// Returns the key which is used to store the room in the roomsHash.
    /// </summary>
    public string GenerateRoomHash()
    {
        return "(" + xMatrix + "," + yMatrix + ")";
    }
}
