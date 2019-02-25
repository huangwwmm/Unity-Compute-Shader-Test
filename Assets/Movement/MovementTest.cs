using System.Runtime.InteropServices;
using UnityEngine;

public class MovementTest : MonoBehaviour
{
    private const int ASYNC_GPU_READBACK_REQUEST_COUNT = 4;

    public GameObject RolePrefab;
    public int RoleCount;
    public Vector2 GroundHalfSize;
    public ComputeShader MovementComputeShader;
    public float MoveSpeed;

    private ComputeBuffer m_CB_I_RoleCommand;
    private CB_I_RoleCommand[] m_RoleCommands;
    private ComputeBuffer m_CB_IO_RoleState;
    private CB_IO_RoleState[] m_RoleStates;
    private ComputeBuffer m_CB_O_RoleResult;
    private CB_O_RoleResult[] m_RoleResults;
    
    private Transform[] m_Roles;

    private UnityEngine.Rendering.AsyncGPUReadbackRequest[] m_AsyncGPUReadbackRequests;
    private int m_NextRequestIdx;

    protected void Awake()
    {
        m_Roles = new Transform[RoleCount];
        m_RoleStates = new CB_IO_RoleState[RoleCount];
        m_RoleCommands = new CB_I_RoleCommand[RoleCount];
        m_RoleResults = new CB_O_RoleResult[RoleCount];
        for (int iRole = 0; iRole < RoleCount; iRole++)
        {
            Vector3 position = RandPositionOnGround();
            GameObject iterRole = Instantiate(RolePrefab, position, Quaternion.identity, null);
            m_Roles[iRole] = iterRole.transform;
            m_RoleStates[iRole].Position = position;
            m_RoleCommands[iRole].MoveTo = RandPositionOnGround();
            m_RoleCommands[iRole].Speed = MoveSpeed;
            m_RoleResults[iRole].IsArrival = false;
        }

        m_CB_IO_RoleState = new ComputeBuffer(RoleCount, Marshal.SizeOf(typeof(CB_IO_RoleState)));
        m_CB_IO_RoleState.SetData(m_RoleStates);

        m_CB_I_RoleCommand = new ComputeBuffer(RoleCount, Marshal.SizeOf(typeof(CB_I_RoleCommand)));
        m_CB_I_RoleCommand.SetData(m_RoleCommands);

        m_CB_O_RoleResult = new ComputeBuffer(RoleCount, Marshal.SizeOf(typeof(CB_O_RoleResult)));
        m_CB_O_RoleResult.SetData(m_RoleResults);

        m_AsyncGPUReadbackRequests = new UnityEngine.Rendering.AsyncGPUReadbackRequest[ASYNC_GPU_READBACK_REQUEST_COUNT];
        m_NextRequestIdx = 0;
        m_AsyncGPUReadbackRequests[m_NextRequestIdx++] = UnityEngine.Rendering.AsyncGPUReadback.Request(m_CB_O_RoleResult);
    }

    protected void Update()
    {
        for (int iRequest = 0; iRequest < ASYNC_GPU_READBACK_REQUEST_COUNT; ++iRequest)
        {
            if (m_AsyncGPUReadbackRequests[iRequest].done 
                && !m_AsyncGPUReadbackRequests[iRequest].hasError)
            {
                Unity.Collections.NativeArray<CB_O_RoleResult> resultBuffers = m_AsyncGPUReadbackRequests[iRequest].GetData<CB_O_RoleResult>();
                for (int iResult = 0; iResult < m_RoleResults.Length; ++iResult)
                {
                    m_RoleResults[iResult] = resultBuffers[iResult];
                }
                m_NextRequestIdx = iRequest;
                break;
            }
        }
        for (int iRequest = 0; iRequest < ASYNC_GPU_READBACK_REQUEST_COUNT; ++iRequest)
        {
            if (m_AsyncGPUReadbackRequests[m_NextRequestIdx].done)
            {
                m_AsyncGPUReadbackRequests[m_NextRequestIdx] = UnityEngine.Rendering.AsyncGPUReadback.Request(m_CB_O_RoleResult);
                break;
            }
            ++m_NextRequestIdx;
            m_NextRequestIdx %= ASYNC_GPU_READBACK_REQUEST_COUNT;
        }

    }

    private Vector3 RandPositionOnGround()
    {
        return new Vector3(Random.Range(-GroundHalfSize.x, GroundHalfSize.x)
            , 1.0f
            , Random.Range(-GroundHalfSize.y, GroundHalfSize.y));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CB_I_RoleCommand
    {
        public Vector3 MoveTo;
        public float Speed;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CB_IO_RoleState
    {
        public Vector3 Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CB_O_RoleResult
    {
        public bool IsArrival;
    }
}