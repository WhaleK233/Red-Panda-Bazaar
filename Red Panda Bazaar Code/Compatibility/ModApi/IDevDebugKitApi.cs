namespace Red_Panda_Bazaar_Code.Compatibility.ModApi;

/// <summary>Dev Debug Kit（WhaleK233.DevDebugKit）暴露的调试状态 API。</summary>
public interface IDevDebugKitApi
{
    /// <summary>调试模式当前是否开启。</summary>
    bool IsEnabled { get; }
}
