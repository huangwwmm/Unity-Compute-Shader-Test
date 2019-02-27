using System.Runtime.InteropServices;
using UnityEngine;

public class MovementTest : MonoBehaviour
{
    private const int ASYNC_GPU_READBACK_REQUEST_COUNT = 4;
    private int INT_TRUE = 1;
    private int INT_FALSE = 0;
    private static readonly int SHADER_PROPERTYID_DELTATIME = Shader.PropertyToID("_DeltaTime");

    public GameObject RolePrefab;
    public int RoleCount;
    public Vector2 GroundHalfSize;
    public ComputeShader MovementComputeShader;
    public float MoveSpeed;

    private RoleTest[] m_Roles;

    private CB_I_RoleCommand[] m_RoleCommands;
    private ComputeBuffer m_CB_I_RoleCommand;

    private CB_IO_RoleState[] m_RoleStates;
    private ComputeBuffer m_CB_IO_RoleState;

    /// <summary>
    /// UNDONE 当前帧发送的Request，下一帧未必完成。所以这里要改成数组，一次发多个，以保证每帧都能拿到数据
    /// </summary>
    private UnityEngine.Rendering.AsyncGPUReadbackRequest m_AsyncGPUReadbackRequest;
    private int m_CS_UpdateKernel;

    protected void Awake()
    {
        m_Roles = new RoleTest[RoleCount];
        m_RoleStates = new CB_IO_RoleState[RoleCount];
        m_RoleCommands = new CB_I_RoleCommand[RoleCount];
        for (int iRole = 0; iRole < RoleCount; iRole++)
        {
            Vector3 position = RandPositionOnGround();
            GameObject iterRole = Instantiate(RolePrefab, position, Quaternion.identity, null);
            m_Roles[iRole] = iterRole.transform.GetComponent<RoleTest>();
            m_RoleStates[iRole].Position = position;
            m_RoleStates[iRole].IsArrival = INT_FALSE;
            m_RoleCommands[iRole].MoveTo = RandPositionOnGround();
            m_RoleCommands[iRole].Speed = MoveSpeed;
            m_Roles[iRole].SetMoveTo(m_RoleCommands[iRole].MoveTo);
        }

        m_CB_IO_RoleState = new ComputeBuffer(RoleCount, Marshal.SizeOf(typeof(CB_IO_RoleState)));
        m_CB_IO_RoleState.SetData(m_RoleStates);

        m_CB_I_RoleCommand = new ComputeBuffer(RoleCount, Marshal.SizeOf(typeof(CB_I_RoleCommand)));
        m_CB_I_RoleCommand.SetData(m_RoleCommands);

        m_AsyncGPUReadbackRequest = UnityEngine.Rendering.AsyncGPUReadback.Request(m_CB_IO_RoleState);

        m_CS_UpdateKernel = MovementComputeShader.FindKernel("Update");
        MovementComputeShader.SetBuffer(m_CS_UpdateKernel, "RoleStates", m_CB_IO_RoleState);
        MovementComputeShader.SetBuffer(m_CS_UpdateKernel, "RoleCommands", m_CB_I_RoleCommand);
    }

    protected void Update()
    {
        if (m_AsyncGPUReadbackRequest.done)
        {
            if (!m_AsyncGPUReadbackRequest.hasError)
            {
                Unity.Collections.NativeArray<CB_IO_RoleState> resultBuffers = m_AsyncGPUReadbackRequest.GetData<CB_IO_RoleState>();
                for (int iResult = 0; iResult < m_RoleStates.Length; ++iResult)
                {
                    m_RoleStates[iResult] = resultBuffers[iResult];
                    m_Roles[iResult].transform.position = m_RoleStates[iResult].Position;
                    if (m_RoleStates[iResult].IsArrival == INT_TRUE)
                    {
                        m_RoleCommands[iResult].MoveTo = RandPositionOnGround();
                        m_Roles[iResult].SetMoveTo(m_RoleCommands[iResult].MoveTo);
                    }
                }
            }
            else
            {
                Debug.LogError("AsyncGPUReadback Error");
            }
            m_AsyncGPUReadbackRequest = UnityEngine.Rendering.AsyncGPUReadback.Request(m_CB_IO_RoleState);
        }

        m_CB_I_RoleCommand.SetData(m_RoleCommands);
        MovementComputeShader.SetFloat(SHADER_PROPERTYID_DELTATIME, Time.deltaTime);

        MovementComputeShader.Dispatch(m_CS_UpdateKernel, 32, 1, 1);
    }

    private Vector3 RandPositionOnGround()
    {
        return new Vector3(Random.Range(-GroundHalfSize.x, GroundHalfSize.x)
            , 1.0f
            , Random.Range(-GroundHalfSize.y, GroundHalfSize.y));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CB_IO_RoleState
    {
        public Vector3 Position;
        public int IsArrival;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CB_I_RoleCommand
    {
        public Vector3 MoveTo;
        public float Speed;
    }
}