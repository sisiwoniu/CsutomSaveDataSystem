#if UNITY_EDITOR
using UnityEngine;

public class DebugSaveDataSlotUI : MonoBehaviour {
    
    private enum DebugSaveDataCon {
        Save = 0,
        Load,
        Delete
    }

    [SerializeField]
    private DebugSaveDataCon Type = DebugSaveDataCon.Save;

    [SerializeField, Range(1, 10)]
    private int Index = 1;

    public void OnClick() {
        switch(Type) {
            case DebugSaveDataCon.Save:
                DebugSaveDataManager.Instance.Save(Index);
                break;
            case DebugSaveDataCon.Load:
                DebugSaveDataManager.Instance.Load(Index);
                break;
            case DebugSaveDataCon.Delete:
                DebugSaveDataManager.Instance.Delete(Index);
                break;
        }
    }
}
#endif
