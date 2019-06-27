using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public class MazeGenerator : MonoBehaviour
{
    #region Public

    public GameObject m_wall;
    public GameObject m_line;
    public int m_circleCount;
    public float m_generationSpeed;
    public GameObject m_case;
    public GameObject m_generator;
    public GameObject m_mapHider;
    [Range(1, 99)]public float m_generateLongLines;
    public LayerMask m_wallMask;
    public GameObject m_playerSpawn;
    public List<GameObject> m_trapsAndMonsters;

    #endregion

    #region System

    private void Awake()
    {
        transform = GetComponent<Transform>();
        _generatorTransforms = new List<Transform>();
        _generatorXPos = new List<int>();
        _generatorYPos = new List<int>();
        _gameController = GetComponentInParent<Transform>();
        GC = FindObjectOfType<GameController>();
        _player = FindObjectOfType<PlayerController>().gameObject.GetComponent<Transform>();
    }

    private void Start()
    {
        _actualLane = 0;
        _actualAngle = 360;
        
        float casesCount = 60 / Puissance2(m_circleCount);
        casesCount = 360 / casesCount;

        _casesTransform = new Transform[(int)casesCount, m_circleCount];
        _cases = new bool[(int)casesCount, m_circleCount];
        Up = true;
        Down = true;
        Left = true;
        Right = true;
        OldWay = null;
    }

    private void Update()
    {
        if (_actualLane <= m_circleCount + 1)
        {
            for(int i = 0; i < _actualLane * _actualLane * m_generationSpeed + 1; i++)
                GenerateMazeCases();
        }
        else if (!_generatorCreated)
        {
            _generatorCreated = true;

            YPos = Random.Range(0, m_circleCount);

            float casesCount = 60 / Puissance2(YPos + 1);
            casesCount = 360 / casesCount;

            XPos = (int)Random.Range(0, casesCount);

            int x = Random.Range(0, 6);
            RaycastHit Hit;
            Physics.Raycast(transform.position, _casesTransform[x, 0].position, out Hit, Mathf.Infinity, m_wallMask);
            Destroy(Hit.collider.gameObject);

            x = Random.Range(0, (int)casesCount);
            Physics.Raycast(_casesTransform[x, m_circleCount - 1].position, _casesTransform[x, m_circleCount - 1].forward, out Hit, Mathf.Infinity, m_wallMask);
            Destroy(Hit.collider.gameObject);

            GameObject NewPlayerSpawn = Instantiate(m_playerSpawn, _gameController);
            NewPlayerSpawn.transform.position = _casesTransform[x, m_circleCount - 1].position + _casesTransform[x, m_circleCount - 1].forward * 2;
            NewPlayerSpawn.transform.rotation = _casesTransform[x, m_circleCount - 1].rotation;

            _player.position = _casesTransform[x, m_circleCount - 1].position + _casesTransform[x, m_circleCount - 1].forward * 3;
            _player.rotation = _casesTransform[x, m_circleCount - 1].rotation;
            
            _actualLane++;
            _actualAngle = 0;

            _angle = 60 / Puissance2(_actualLane);
            _scale = (_actualLane / Puissance2(_actualLane));
            _actualWall = 0;
            _actualAngle += _angle;

            while (_actualAngle < 360 + _angle - .1f)
            {
                NewWall(_actualAngle - _angle / 2, 2 * _actualLane, _scale);
                _actualAngle += _angle;
            }

            foreach(GameObject TaM in m_trapsAndMonsters)
            {
                int Y = Random.Range(0, m_circleCount - 1);
                casesCount = 60 / Puissance2(Y + 1);
                casesCount = 360 / casesCount;
                int X = Random.Range(0, (int)casesCount - 1);

                GameObject NewTaM = Instantiate(TaM, _gameController);
                NewTaM.transform.position = _casesTransform[X, Y].position;
                NewTaM.transform.rotation = _casesTransform[X, Y].rotation;
            }
        }
        if (_generatorCreated && !_generationCompleted)
        {
            _generationSpeed += m_generationSpeed / 5 + .01f * _generationSpeed;
            for (int i = 0; i < 1 + 1 * _generationSpeed; i++)
                GenerateMaze();
        }
        if (_generationCompleted)
        {
            NavMeshObstacle[] Walls = FindObjectsOfType<NavMeshObstacle>();
            foreach (NavMeshObstacle N in Walls)
                N.enabled = true;
            Destroy(this);
        }
    }

    #region Generate Maze Cases

    private void GenerateMazeCases()
    {
        if (_actualLane > m_circleCount + 1)
            return;

        if (_actualAngle >= 360 + _angle)
        {
            _actualLane++;
            _actualAngle = 0;

            _angle = 60 / Puissance2(_actualLane);
            _scale = (_actualLane / Puissance2(_actualLane));
            _actualWall = 0;
            _actualAngle += _angle;
        }
        else if(_actualAngle < 360 + _angle - .1f)
        {
            if (_actualLane < m_circleCount + 1)
            {
                NewLineWall(_actualAngle, 1 + 2 * _actualLane);
                NewCase(_actualAngle - _angle / 2, 1 + 2 * _actualLane);
            }
            NewWall(_actualAngle - _angle / 2, 2 * _actualLane, _scale);
            NewMapHider(_actualAngle - _angle / 2, 1 + 2 * _actualLane, _scale);
            _actualAngle += _angle;
        }
    }

    private void NewLineWall(float angle, int distance)
    {
        GameObject NewWall = Instantiate(m_line, transform);
        Transform NWtransform = NewWall.GetComponent<Transform>();
        Quaternion NWrotation = Quaternion.Euler(0, angle, 0);
        NWtransform.rotation = NWrotation;
        NWtransform.position = NWtransform.right * (distance + .1f) + transform.position;
    }

    private void NewWall(float angle, int distance, float scale)
    {
        GameObject NewWall = Instantiate(m_wall, transform);
        Transform NWtransform = NewWall.GetComponent<Transform>();
        Quaternion NWrotation = Quaternion.Euler(0, angle + 90, 0);
        NWtransform.rotation = NWrotation;
        NWtransform.position = NWtransform.forward * distance + transform.position;
        NWtransform.localScale = new Vector3(NWtransform.localScale.x * scale, NWtransform.localScale.y, NWtransform.localScale.z);
    }

    private void NewCase(float angle, int distance)
    {
        GameObject NewWall = Instantiate(m_case, transform);
        Transform NCtransform = NewWall.GetComponent<Transform>();
        Quaternion NCrotation = Quaternion.Euler(0, angle + 90, 0);
        NCtransform.rotation = NCrotation;
        NCtransform.position = NCtransform.forward * distance + transform.position;
        _casesTransform[_actualWall, _actualLane - 1] = NCtransform;
        _actualWall++;
    }

    private void NewMapHider(float angle, int distance, float scale)
    {
        GameObject NewMapHider = Instantiate(m_mapHider, transform);
        Transform NWtransform = NewMapHider.GetComponent<Transform>();
        Quaternion NWrotation = Quaternion.Euler(0, angle + 90, 0);
        NWtransform.rotation = NWrotation;
        NWtransform.position = NWtransform.forward * distance + transform.position + Vector3.up * 10;
        NWtransform.localScale = new Vector3(NWtransform.localScale.x * scale, NWtransform.localScale.y, NWtransform.localScale.z);
        GC._casesCount++;
    }

    #endregion

    #region Generate Maze

    private void GenerateMaze()
    {
        if (_generationCompleted)
            return;

        float casesCount = 60 / Puissance2(YPos + 1);
        casesCount = 360 / casesCount;

        int XposUp = XPos;
        int XposDown = XPos;

        if ((int)Puissance2(YPos + 1) > (int)Puissance2(YPos))
            XposUp = XPos / 2;

        if ((int)Puissance2(YPos + 1) < (int)Puissance2(YPos + 2))
            if (Random.Range(0, 100) > 50)
                XposDown = XPos * 2;
            else
                XposDown = XPos * 2 + 1;

        if (YPos - 1 >= 0)
        {
            Up = !_cases[XposUp, YPos - 1];
        }
        else
        {
            Up = false;
        }
        if (YPos + 1 <= m_circleCount - 1)
        {
            Down = !_cases[XposDown, YPos + 1];
        }
        else
        {
            Down = false;
        }
        if (XPos + 1 <= casesCount - 1)
        {
            Left = !_cases[XPos + 1, YPos];
        }
        else
        {
            Left = !_cases[0, YPos];
        }
        if (XPos - 1 >= 0)
        {
            Right = !_cases[XPos - 1, YPos];
        }
        else
        {
            Right = !_cases[(int)casesCount - 1, YPos];
        }

        if (Up || Down || Left || Right)
        {
            int wayCount = 0;
            if (Up)
                wayCount++;
            if (Down)
                wayCount++;
            if (Left)
                wayCount++;
            if (Right)
                wayCount++;

            Dictionary<string, float> PossibleWay = new Dictionary<string, float>();
            PossibleWay.Add("Up", 0);
            PossibleWay.Add("Down", 0);
            PossibleWay.Add("Left", 0);
            PossibleWay.Add("Right", 0);

            if (OldWay == "Up" && !Up)
                OldWay = null;
            if (OldWay == "Down" && !Down)
                OldWay = null;
            if (OldWay == "Left" && !Left)
                OldWay = null;
            if (OldWay == "Right" && !Right)
                OldWay = null;

            if (OldWay != null)
            {
                PossibleWay[OldWay] = m_generateLongLines;
                if (OldWay != "Up" && Up)
                    PossibleWay["Up"] = (100 - m_generateLongLines) / (wayCount - 1);
                if (OldWay != "Down" && Down)
                    PossibleWay["Down"] = (100 - m_generateLongLines) / (wayCount - 1);
                if (OldWay != "Left" && Left)
                    PossibleWay["Left"] = (100 - m_generateLongLines) / (wayCount - 1);
                if (OldWay != "Right" && Right)
                    PossibleWay["Right"] = (100 - m_generateLongLines) / (wayCount - 1);
            }
            else
            {
                if (Up)
                    PossibleWay["Up"] = 100 / wayCount;
                if (Down)
                    PossibleWay["Down"] = 100 / wayCount;
                if (Left)
                    PossibleWay["Left"] = 100 / wayCount;
                if (Right)
                    PossibleWay["Right"] = 100 / wayCount;
            }
            float Way = Random.Range(0, 100);

            if (Way < PossibleWay["Up"])
            {
                YPos--;
                XPos = XposUp;
                OldWay = "Up";
            }
            else if (Way < PossibleWay["Up"] + PossibleWay["Down"])
            {
                YPos++;
                XPos = XposDown;
                OldWay = "Down";
            }
            else if (Way < PossibleWay["Up"] + PossibleWay["Down"] + PossibleWay["Left"])
            {
                XPos++;
                OldWay = "Left";
            }
            else
            {
                XPos--;
                OldWay = "Right";
            }

            casesCount = 60 / Puissance2(YPos + 1);
            casesCount = 360 / casesCount;

            if (XPos >= (int)casesCount)
                XPos = 0;
            if (XPos < 0)
                XPos = (int)casesCount - 1;

            GameObject G = Instantiate(m_generator, transform);
            _generatorTransforms.Add(G.GetComponent<Transform>());
            _generatorTransforms[_generatorTransforms.Count - 1].position = _casesTransform[XPos, YPos].position;
            _cases[XPos, YPos] = true;
            _generatorXPos.Add(XPos);
            _generatorYPos.Add(YPos);

            if (_generatorTransforms.Count < 2)
                return;

            RaycastHit Hit;
            Physics.Raycast(_generatorTransforms[_generatorTransforms.Count - 2].position, _generatorTransforms[_generatorTransforms.Count - 1].position - _generatorTransforms[_generatorTransforms.Count - 2].position, out Hit, Mathf.Infinity, m_wallMask);
            Destroy(Hit.collider.gameObject);
            Debug.DrawRay(_generatorTransforms[_generatorTransforms.Count - 2].position, _generatorTransforms[_generatorTransforms.Count - 1].position - _generatorTransforms[_generatorTransforms.Count - 2].position, Color.green);
        }
        else
        {
            if (_generatorTransforms.Count == 0 && !_generationCompleted)
            {
                _generationCompleted = true;
            }

            if (_casesTransform[XPos, YPos] == null && !_debug)
            {
                SceneManager.LoadScene("Main");
                _debug = true;
                return;
            }
            if (_casesTransform[XPos, YPos] == null)
                Destroy(_casesTransform[XPos, YPos].gameObject);
            if (_generatorTransforms.Count > 0)
                Destroy(_generatorTransforms[_generatorTransforms.Count - 1].gameObject);
            if (_generatorTransforms.Count > 0)
                _generatorTransforms.RemoveAt(_generatorTransforms.Count - 1);
            if (_generatorXPos.Count > 0)
                _generatorXPos.RemoveAt(_generatorXPos.Count - 1);
            if (_generatorYPos.Count > 0)
                _generatorYPos.RemoveAt(_generatorYPos.Count - 1);

            if(_generatorXPos.Count > 0)
                XPos = _generatorXPos[_generatorTransforms.Count - 1];
            if(_generatorYPos.Count > 0)
                YPos = _generatorYPos[_generatorTransforms.Count - 1];
        }
    }

    #endregion

    #endregion

    #region Tools

    public float Puissance2(int Number)
    {
        float puissance = 1;
        for (int i = 1; i <= Number; i++)
            if (i > puissance + puissance / 3)
                puissance *= 2;
        return puissance;
    }

    #endregion

    #region Private
    
    private new Transform transform;
    private int _actualLane;
    private float _actualAngle;
    private int _actualWall;
    private float _angle;
    private float _scale;
    private Transform[,] _casesTransform;
    private bool[,] _cases;
    private List<int> _generatorXPos;
    private List<int> _generatorYPos;
    private int XPos;
    private int YPos;
    private List<Transform> _generatorTransforms;
    private bool _generatorCreated;
    private int OldYPos;
    private string OldWay;
    private bool Up;
    private bool Down;
    private bool Left;
    private bool Right;
    private bool _generationCompleted;
    private float _generationSpeed;
    private bool _debug;
    private Transform _gameController;
    public Transform _player;
    private GameController GC;

    #endregion
}