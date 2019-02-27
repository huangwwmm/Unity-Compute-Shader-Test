using System.Runtime.InteropServices;
using UnityEngine;

public class MovementTest : MonoBehaviour
{
    private const int ASYNC_GPU_READBACK_REQUEST_COUNT = 4;
    private int INT_TRUE = 1;
    private int INT_FALSE = 0;
    private static readonly int SHADER_PROPERTYID_DELTATIME = Shader.PropertyToID("_DeltaTime");
    private static Bounds UNLIMITED_BOUNDS = new Bounds(Vector3.zero, new Vector3(999999f, 999999f, 999999f));

    public Camera MainCamera;
    public Mesh RoleMesh;
    public Material RoleMaterial;
    public int RoleCount;
    public Vector2 GroundHalfSize;
    public ComputeShader MovementComputeShader;
    public float MoveSpeed;

    private RoleState[] m_RoleStates;
    private ComputeBuffer m_CB_RoleState;

    private RoleCommand[] m_RoleCommands;
    private ComputeBuffer m_CB_RoleCommand;

    private RoleResult[] m_RoleResults;
    private ComputeBuffer m_CB_RoleResult;

    private uint[] m_RoleArgs;
    private ComputeBuffer m_CB_RoleArgs;

    /// <summary>
    /// UNDONE 当前帧发送的Request，下一帧未必完成。所以这里要改成数组，一次发多个，以保证每帧都能拿到数据
    /// </summary>
    private UnityEngine.Rendering.AsyncGPUReadbackRequest m_AsyncGPUReadbackRequest;
    private int m_CS_UpdateKernel;

    protected void Awake()
    {
        m_CS_UpdateKernel = MovementComputeShader.FindKernel("Update");

        m_RoleStates = new RoleState[RoleCount];
        m_RoleCommands = new RoleCommand[RoleCount];
        m_RoleResults = new RoleResult[RoleCount];
        for (int iRole = 0; iRole < RoleCount; iRole++)
        {
            m_RoleStates[iRole].Position = RandPositionOnGround();
            m_RoleCommands[iRole].MoveTo = RandPositionOnGround();
            m_RoleCommands[iRole].Speed = MoveSpeed;
            m_RoleResults[iRole].IsArrival = INT_FALSE;
        }

        m_CB_RoleState = new ComputeBuffer(RoleCount, Marshal.SizeOf(typeof(RoleState)));
        m_CB_RoleState.SetData(m_RoleStates);

        m_CB_RoleCommand = new ComputeBuffer(RoleCount, Marshal.SizeOf(typeof(RoleCommand)));
        m_CB_RoleCommand.SetData(m_RoleCommands);

        m_CB_RoleResult = new ComputeBuffer(RoleCount, Marshal.SizeOf(typeof(RoleResult)));
        m_CB_RoleResult.SetData(m_RoleResults);

        m_RoleArgs = new uint[5] { RoleMesh.GetIndexCount(0), (uint)RoleCount, 0, 0, 0 };
        m_CB_RoleArgs = new ComputeBuffer(1
            , (m_RoleArgs.Length * Marshal.SizeOf(typeof(uint)))
            , ComputeBufferType.IndirectArguments);
        m_CB_RoleArgs.SetData(m_RoleArgs);

        MovementComputeShader.SetBuffer(m_CS_UpdateKernel, "RoleStates", m_CB_RoleState);
        MovementComputeShader.SetBuffer(m_CS_UpdateKernel, "RoleCommands", m_CB_RoleCommand);
        MovementComputeShader.SetBuffer(m_CS_UpdateKernel, "RoleResults", m_CB_RoleResult);
        RoleMaterial.SetBuffer("RoleStates", m_CB_RoleState);

        UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    protected void OnDestroy()
    {
        UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;

        m_CB_RoleCommand.Release();
        m_CB_RoleState.Release();
        m_CB_RoleResult.Release();
        m_CB_RoleArgs.Release();
    }

    protected void LateUpdate()
    {
        bool needSendCommand = false;
        if (m_AsyncGPUReadbackRequest.done)
        {
            if (!m_AsyncGPUReadbackRequest.hasError)
            {
                Unity.Collections.NativeArray<RoleResult> resultBuffers = m_AsyncGPUReadbackRequest.GetData<RoleResult>();
                resultBuffers.CopyTo(m_RoleResults);
                for (int iResult = 0; iResult < m_RoleResults.Length; ++iResult)
                {
                    if (m_RoleResults[iResult].IsArrival == INT_TRUE)
                    {
                        m_RoleCommands[iResult].MoveTo = RandPositionOnGround();
                        needSendCommand = true;
                    }
                }
            }
            else
            {
                Debug.LogError("AsyncGPUReadback Error");
            }
            m_AsyncGPUReadbackRequest = UnityEngine.Rendering.AsyncGPUReadback.Request(m_CB_RoleResult);
        }

        if (needSendCommand)
        {
            m_CB_RoleCommand.SetData(m_RoleCommands);
        }
        MovementComputeShader.SetFloat(SHADER_PROPERTYID_DELTATIME, Time.deltaTime);

        MovementComputeShader.Dispatch(m_CS_UpdateKernel, RoleCount, 1, 1);

        Draw(MainCamera);
    }

    private void OnSceneGUI(UnityEditor.SceneView sceneView)
    {
        Draw(sceneView.camera);
    }

    private Vector3 RandPositionOnGround()
    {
        return new Vector3(Random.Range(-GroundHalfSize.x, GroundHalfSize.x)
            , 1.0f
            , Random.Range(-GroundHalfSize.y, GroundHalfSize.y));
    }

    private void Draw(Camera camera)
    {
        Graphics.DrawMeshInstancedIndirect(RoleMesh
            , 0
            , RoleMaterial
            , UNLIMITED_BOUNDS
            , m_CB_RoleArgs
            , 0
            , null
            , UnityEngine.Rendering.ShadowCastingMode.Off
            , false
            , 0
            , camera);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RoleState
    {
        public Vector3 Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RoleCommand
    {
        public Vector3 MoveTo;
        public float Speed;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RoleResult
    {
        public int IsArrival;
    }
}