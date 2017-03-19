using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The Floor is responsible for generating the layout of each level of the game. The Floor also maintains information about the current level which is used
/// by other parts of the game, namely individual rooms within each level.
/// </summary>
public class Floor : MonoBehaviour {

    // Represents the current floor the player is on. The current floor impacts how many rooms there are.
    private int currentFloor;

    // The basic room (to be filled with enemies).
    private GameObject roomBase;

    // List of all the rooms.
    private IList<Room> rooms;
    // 2D matrix of the rooms.
    private byte[,] roomsMatrix;
    // Hash of the rooms.
    private IDictionary<string, Room> roomsHash;
    /// <summary>
    /// Reference to the room in which the player is currently located.
    /// </summary>
    private Room activeRoom;

    // Shape Shooter title text.
    public GameObject titleText;

    // Reference to the Player GameObject.
    public GameObject player;

    // Size of a room. Used for positioning the rooms as they're generated.
    private Vector3 roomSize;

    private float timeToRegenerate = 30.0f;
    private float timeLeftToRegenerate;


	// Initializes appropriate variables.
	void Start () {
        currentFloor = 1; // We begin on the first floor (one-indexed).
        roomBase = Resources.Load("Prefabs/Floor/Room") as GameObject;
        // Store the size of the room. 
        roomSize = roomBase.transform.FindChild("Quad").GetComponent<Renderer>().bounds.size;
        roomsHash = new Dictionary<string, Room>();
        GenerateFloor();
        timeLeftToRegenerate = timeToRegenerate;
    }

    void Update()
    {
        timeLeftToRegenerate -= Time.deltaTime;
        // Determine if we need to generate more enemies.
        if (timeLeftToRegenerate <= 0)
        {
            print("GENERATING ENEMIES!");   
            timeLeftToRegenerate = timeToRegenerate;
            for (int i = 1; i < rooms.Count; i++)
            {
                rooms[i].GenerateEnemies();
                // Slowly increase the frequency in which we regenerate enemies.
                if (timeToRegenerate > 10.0f) timeToRegenerate -= 1.0f;
            }
        }

        // Check the current room and the surrounding rooms for the player. This avoids having to check EVERY room every frame.
        activeRoom.IsPlayerInThisRoom();
        if (activeRoom.North != null && activeRoom.North.IsPlayerInThisRoom()) activeRoom = activeRoom.North;
        if (activeRoom.South != null && activeRoom.South.IsPlayerInThisRoom()) activeRoom = activeRoom.South;
        if (activeRoom.East != null && activeRoom.East.IsPlayerInThisRoom()) activeRoom = activeRoom.East;
        if (activeRoom.West != null && activeRoom.West.IsPlayerInThisRoom()) activeRoom = activeRoom.West;

    }

    // Algorithm which generates the layout for the current floor.
    private void GenerateFloor()
    {
        UnityEngine.Profiling.Profiler.BeginSample("GenerateFloor()");
        // Calculate how many rooms there are to be based on the current floor.
        int numRooms = CalculateNumberOfRooms(currentFloor);
        // We left shift by one (optimized multiply by two) and add one to avoid edge cases when checking neighbors. We don't have to account for the border,
        // as the array is big enough that if it generated all of the rooms in a straight line from the middle to the edge, there'd be one spot remaining.
        int matrixDimension = (numRooms << 1) + 1; 
        // Instantiate the array which maintains a grid-layout of the floor to determine which spots are open for room generation.
        roomsMatrix = new byte[matrixDimension, matrixDimension];
        // Create the roomID variable. It is incremented each time a room is assigned a roomID (which uses this variable).
        int roomID = 0;
        // Instantiate the list of rooms. Each time a room is created, it is added to this list.
        rooms = new List<Room>();
        // Instantiate the first room. We will add a Room component to this GameObject, 
        // and then maintain a reference to that room by which we can modify the room.
        GameObject firstGO = Instantiate(roomBase);
        Room first = firstGO.AddComponent<Room>();
        // Assign a roomID to the first room, and give it an x and y position as well as the position of the room in the room matrix.
        first.roomID = roomID++;
        first.x = this.transform.position.x;
        first.y = this.transform.position.y;
        first.xMatrix = matrixDimension >> 1;
        first.yMatrix = matrixDimension >> 1;
        // Add the new room to the rooms hash, rooms matrix, and rooms list.
        roomsHash.Add(GenerateRoomHash(first.xMatrix, first.yMatrix), first);
        roomsMatrix[first.xMatrix, first.yMatrix] = 0;
        rooms.Add(first);
        for (int i = 0; i < numRooms; i++)
        {
            GameObject newRoomGO = Instantiate(roomBase);
            Room newRoom = newRoomGO.AddComponent<Room>();
            newRoom.roomID = roomID++;
            bool done = false;
            while (!done)
            {
                // Pick a random room from the list of rooms. We will place this new room next to it.
                int index = Random.Range(0, rooms.Count);
                var possibleDirections = new List<byte>();
                for (byte b = 0; b < 4; b++)
                {
                    if ((rooms[index].Doors & (1 << b)) == 0) possibleDirections.Add(b);
                }
                // If there are no possible directions in which we can add a room, try a different room.
                if (possibleDirections.Count == 0)
                {
                    Debug.Log("No possible rooms for Room " + rooms[index].roomID + ". Trying different room instead.");
                    continue;
                }
                // Pick a direction from the possible directions.
                int direction = possibleDirections[Random.Range(0, possibleDirections.Count)];
                // If we got this far, then it is safe to assume the spot on which we wish 
                // to add a room is free. 
                // 
                // First, we adjust the door variable of the new and
                // existing rooms since we are connecting them. 
                // 
                // Then we calculate the x and y coordinates of the new 
                // room using the existing room's coordinates. 
                //
                // Then, we update the grid of rooms to make note of the new room. 
                // Then, we check neighboring  rooms to see if we need to add doors 
                // (and if so, we add the doors).

                // We multiply the value returned by GetXValueBasedOnDirection() or GetYValueBasedOnDirection()
                // by the roomSize and add that to the x or y value of the existing room respectively. This is becauase
                // the GetValueBasedOnDirection() method will return -1, 0, or 1 and multiplying this to the appropriate roomSize
                // component will give us the correct calculation.
                newRoom.x = rooms[index].x + (GetXValueBasedOnDirection(direction) * roomSize.x);
                newRoom.y = rooms[index].y + (GetYValueBasedOnDirection(direction) * roomSize.y);
                newRoom.xMatrix = rooms[index].xMatrix + GetXValueBasedOnDirection(direction);
                newRoom.yMatrix = rooms[index].yMatrix + GetYValueBasedOnDirection(direction);
                // Add the new room to the rooms hash.
                roomsHash.Add(GenerateRoomHash(newRoom.xMatrix, newRoom.yMatrix), newRoom);
                // Update the doors data for the new room and the existing room.
                newRoom.Doors = (byte)(1 << GetCorrespondingDirection(direction));
                rooms[index].Doors = (byte)(rooms[index].Doors | (1 << direction));
                rooms.Add(newRoom);

                // Using the roomHash, update the North, South, East, and West references of the new room and its neighbors. 
                 Room test;
                if (roomsHash.TryGetValue(GenerateRoomHash(newRoom.xMatrix + 1, newRoom.yMatrix), out test))
                {
                    newRoom.East = test;
                    test.West = newRoom;
                }
                if (roomsHash.TryGetValue(GenerateRoomHash(newRoom.xMatrix - 1, newRoom.yMatrix), out test))
                {
                    newRoom.West = test;
                    test.East = newRoom;
                }
                if (roomsHash.TryGetValue(GenerateRoomHash(newRoom.xMatrix, newRoom.yMatrix + 1), out test))
                {
                    newRoom.North = test;
                    test.South = newRoom;
                }
                if (roomsHash.TryGetValue(GenerateRoomHash(newRoom.xMatrix, newRoom.yMatrix - 1), out test))
                {
                    newRoom.South = test;
                    test.North = newRoom;
                }

                // Check the neighboring spots to see if we need to add doors. Note that if the new room or the existing is a 
                // special room, we do not check the neighboring rooms because special rooms only have one way into the room.
                if (!((newRoom.Doors & (1 << 4)) == 1))
                {
                    // We check if the room does not equal zero because there exists a room iff the node 
                    // of the matrix has the value 1, 2, 4, 8, or any combination of adding those together. 
                    if (roomsMatrix[newRoom.xMatrix, newRoom.yMatrix - 1] != 0 && (roomsMatrix[newRoom.xMatrix, newRoom.yMatrix - 1] & (1 << 4)) == 0) // North (top) neighbor.
                    {
                        roomsMatrix[newRoom.xMatrix, newRoom.yMatrix - 1] = (byte)(roomsMatrix[newRoom.xMatrix, newRoom.yMatrix - 1] | (1 << 3));
                        newRoom.Doors = (byte)(newRoom.Doors | (1 << 2));
                    }
                    if (roomsMatrix[newRoom.xMatrix, newRoom.yMatrix + 1] != 0 && (roomsMatrix[newRoom.xMatrix, newRoom.yMatrix + 1] & (1 << 4)) == 0) // South (bottom) neighbor.
                    {
                        roomsMatrix[newRoom.xMatrix, newRoom.yMatrix + 1] = (byte)(roomsMatrix[newRoom.xMatrix, newRoom.yMatrix + 1] | (1 << 2));
                        newRoom.Doors = (byte)(newRoom.Doors | (1 << 3));
                    }
                    if (roomsMatrix[newRoom.xMatrix + 1, newRoom.yMatrix] != 0 && (roomsMatrix[newRoom.xMatrix + 1, newRoom.yMatrix] & (1 << 4)) == 0) // East (right) neighbor.
                    {
                        roomsMatrix[newRoom.xMatrix + 1, newRoom.yMatrix] = (byte)(roomsMatrix[newRoom.xMatrix + 1, newRoom.yMatrix] | (1 << 0));
                        newRoom.Doors = (byte)(newRoom.Doors | (1 << 1));
                    }
                    if (roomsMatrix[newRoom.xMatrix - 1, newRoom.yMatrix] != 0 && (roomsMatrix[newRoom.xMatrix - 1, newRoom.yMatrix] & (1 << 4)) == 0) // West (left) neighbor.
                    {
                        roomsMatrix[newRoom.xMatrix - 1, newRoom.yMatrix] = (byte)(roomsMatrix[newRoom.xMatrix - 1, newRoom.yMatrix] | (1 << 1));
                        newRoom.Doors = (byte)(newRoom.Doors | (1 << 0));
                    }
                }

                // We want the outer loop to exist once we break out of the     
                // inner loop (the inner loop being the while(tries > 0) loop.
                done = true;
            }
        }
        // Move the rooms into their proper positions and place the doors.
        for (int i = 0; i < rooms.Count; i++)
        {
            // Create a reference to the game object to which the room is attached.
            GameObject gameObject = rooms[i].gameObject;
            // Adjust the room's position via the transform.position field.
            gameObject.transform.position = new Vector3(rooms[i].x, rooms[i].y, 0);
            // Place the doors appropriately.
            rooms[i].PlaceWalls();
            rooms[i].PlaceDoors();
            rooms[i].doorsDebug = roomsMatrix[rooms[i].xMatrix, rooms[i].yMatrix];
            // We don't want enemies in the first room.
            if (i != 0) rooms[i].GenerateEnemies();
        }
        // Instantiate the player. 
        Instantiate(player, this.transform.position, Quaternion.identity);
        // The player will be created in the zero'th room, so that is the activeRoom.
        activeRoom = rooms[0];
        // If we're on the first floor, then instantiate the title text.
        if (currentFloor == 1) Instantiate(titleText, new Vector3(0, 4, 0), Quaternion.identity);
        UnityEngine.Profiling.Profiler.EndSample();
    }

    // The length/width of the floor array is the floor number multiplied by ten
    // plus or minus two times a random number between one and the floor number.
    private int CalculateNumberOfRooms(int floor)
    {
        return (floor * 10) + Random.Range(1, floor);
    }

    /// <summary>
    /// Helper method which will return the corresponding direction based on the given direction.
    /// 3 (North) corresponds with 2 (South).
    /// 2 (South) corresponds with 3 (North).
    /// 1 (East) corresponds with 0 (West).
    /// 0 (West) corresponds with 1 (East).
    /// </summary>
    private int GetCorrespondingDirection(int dir)
    {
        if (dir == 3) return 2;
        if (dir == 2) return 3;
        if (dir == 1) return 0;
        if (dir == 0) return 1;
        return -1; // Should never happen.
    }

    /// <summary>
    /// This method is used when updating/accessing the matrix (grid) of rooms. Given a direction,
    /// this method will return how much you must add or subtract to/from the known x-value to get the
    /// correct x-value.
    /// 
    /// Let's say we were adding a room to the East wall of an existing room. We would be adding this new
    /// room one position to the right of the existing room. This means the new room should be one position 
    /// to the right of the existing room in the matrix, so this will return 1.
    /// </summary>
    private int GetXValueBasedOnDirection(int dir)
    {
        if (dir == 1) return 1; // East.
        if (dir == 0) return -1; // West.
        return 0; // North or South or invalid direction.
    }

    /// <summary>
    /// This method is used when updating/accessing the matrix (grid) of rooms. Given a direction,
    /// this method will return how much you must add or subtract to/from the known y-value to get the
    /// correct y-value.
    /// 
    /// Let's say we were adding a room to the North wall of an existing room. We would be adding this new
    /// room one position above the existing room. This means the new room should be one position 
    /// to the above of the existing room in the matrix, so this will return -1. Note that it returns -1
    /// because "up" in the matrix is a lower index than down in the matrix. 
    /// </summary>
    private int GetYValueBasedOnDirection(int dir)
    {
        if (dir == 3) return 1; // North.
        if (dir == 2) return -1; // South.
        return 0; // East or West or invalid direction.
    }

    /// <summary>
    /// Returns true if the given spot is NOT occupied, returns false if it IS occupied.
    /// </summary>
    private bool IsSpotFree(int x, int y)
    {
        return (roomsMatrix[x, y] == 0);
    }

    /// <summary>
    /// Returns the key which is used to store the room in the roomsHash.
    /// </summary>
    public string GenerateRoomHash(int xMatrix, int yMatrix)
    {
        return "(" + xMatrix + "," + yMatrix + ")";
    }

    /// <summary>
    /// Sets the byte at roomsMatrix[x,y] to b. 
    /// </summary>
    /// <param name="b">New byte value.</param>
    public void SetMatrixEntry(int x, int y, byte b)
    {
        roomsMatrix[x, y] = b;
    }

    #region Properties

    /// <summary>
    /// Property which exposes the number of the current floor.
    /// </summary>
    public int CurrentFloor
    {
        get
        {
            return currentFloor;
        }
    }

    /// <summary>
    /// Property which exposes the size of a room.
    /// </summary>
    public Vector3 RoomSize
    {
        get
        {
            return roomSize;
        }
    }

    /// <summary>
    /// Exposes the byte matrix roomsMatrix.
    /// </summary>
    public byte[,] RoomsMatrix
    {
        get
        {
            return roomsMatrix;
        }
    }

    #endregion 

    private class Tuple<T, K>
    {
        T first;
        K second;

        public Tuple(T t, K k)
        {
            first = t;
            second = k;
        }

        public T First
        {
            get
            {
                return first;
            }
            set
            {
                first = value;
            }
        }

        public K Second
        {
            get
            {
                return second;
            }
            set
            {
                second = value;
            }
        }
    } 
}
