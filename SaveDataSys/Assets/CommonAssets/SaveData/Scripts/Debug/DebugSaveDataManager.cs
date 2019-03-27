using GCustomSaveData;

public class DebugSaveDataManager : BaseSaveDataManager<DebugSaveData> {

    //SaveDataのクラスの内部メンバーが変更されるとここも変えないといけない
    protected override void ApplyCacheDataToData() {
        usingData.Name = cacheUsingData.Name;

        usingData.Num = cacheUsingData.Num;

        usingData.TestNum = cacheUsingData.TestNum;
    }

    protected override void OnLoadStartEvent() {
        print("DebugSaveDataManagerのロード開始イベント");
    }

    protected override void OnLoadCompletedEvent(bool LoadSuccessed) {
        if(LoadSuccessed) {
            print("DebugSaveDataManagerのロード成功イベント");
        } else {
            print("DebugSaveDataManagerのロード失敗イベント");
        }
    }

    protected override void OnSaveStartEvent() {
        print("DebugSaveDataManagerのセーブ開始イベント");
    }

    protected override void OnSaveCompletedEvent(string ErrMsg) {
        if(string.IsNullOrEmpty(ErrMsg)) {
            print("DebugSaveDataManagerのセーブ成功イベント");
        } else {
            print("DebugSaveDataManagerのセーブ失敗イベント");
        }
    }

    protected override void OnDeleteCompletedEvent(string ErrMsg) {
        if(string.IsNullOrEmpty(ErrMsg)) {
            print("DebugSaveDataManagerの削除成功イベント");
        } else {
            print("DebugSaveDataManagerの削除失敗イベント");
        }
    }
}
