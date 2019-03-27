using UnityEngine;
using UniRx;
using GCustomSaveData;

//セーブデータマネジャー
public abstract class BaseSaveDataManager<T> : MonoBehaviour where T : BaseSaveData, new() {

    private static bool Inited = false;

    private static BaseSaveDataManager<T> instance;

    [SerializeField]
    private SaveDataType DataType = SaveDataType.Json;

    [SerializeField, Range(1, 10)]
    private int MaxIndex = 1;

    [SerializeField]
    private string ProductName = "TesterSaveManager";

    [SerializeField]
    private bool UseBackUp = true;

    protected T usingData, cacheUsingData, defaultData = null;

    private ISaveDataRecorder<T> saveDataRecorder;

    public static BaseSaveDataManager<T> Instance {
        get {
            return instance ?? (instance = FindObjectOfType<BaseSaveDataManager<T>>());
        }
    }

    //現在使用中のデータ、実はキャッシュで、破棄可能、セーブ走ると適用される
    public T UsingSaveData {
        get {
            return cacheUsingData;
        }
    }

    public bool IsNothing {
        get {
            return saveDataRecorder == null ? true : saveDataRecorder.IsNothing;
        }
    }

    public int SaveDataMaxIndex {
        get {
            return MaxIndex;
        }
    }

    public void Init() {
        if(Inited)
            return;

#if UNITY_EDITOR
        //共通のもの
        saveDataRecorder = new CommonSaveDataRecorder<T>();
#else
        //共通のもの
        saveDataRecorder = new CommonSaveDataRecorder<T>();
#endif
        //ロード完了イベント
        saveDataRecorder.OnLoadCompleted().Subscribe(SaveData => {
            if(SaveData != null) {
                usingData = new T();

                cacheUsingData = new T();

                cacheUsingData.DeepCopy(usingData);
            } else {
                Debug.Log("ロードするデータが存在していない");
            }

            //終了イベント
            OnLoadCompletedEvent(SaveData != null);

            Debug.Log("ロード完了");
        }).AddTo(gameObject);

        //セーブ完了イベント
        saveDataRecorder.OnSaveCompleted().Subscribe(errMsg => {
            //セーブ終了イベント
            OnSaveCompletedEvent(errMsg);

            Debug.Log("セーブ完了");
        }).AddTo(gameObject);

        //削除完了イベント
        saveDataRecorder.OnDeleteCompleted().Subscribe(errMsg => {
            //削除完了イベント
            OnDeleteCompletedEvent(errMsg);
        }).AddTo(gameObject);

        //とりあえずデフォルトデータをセットする
        defaultData = new T();

        usingData = new T();

        usingData.DeepCopy(defaultData);

        cacheUsingData = new T();

        cacheUsingData.DeepCopy(defaultData);

        //セーブデータ初期化
        saveDataRecorder.Init(MaxIndex, DataType, ProductName, UseBackUp);

        Inited = true;
    }

    public void Save(int DataIndex) {
        if(DataIndex > MaxIndex || DataIndex <= 0) {
            Debug.LogWarning("DataIndexの設定値が正しくない");
        } else {
            //セーブ開始イベント
            OnSaveStartEvent();

            //キャッシュしていたデータを適用する
            ApplyCacheDataToData();

            saveDataRecorder.Save(DataIndex - 1, usingData);
        }
    }

    public void Load(int DataIndex) {
        if(DataIndex > MaxIndex || DataIndex <= 0) {
            Debug.LogWarning("DataIndexの設定値が正しくない");
        } else {
            //ロード開始イベント
            OnLoadStartEvent();

            saveDataRecorder.Load(DataIndex - 1);
        }
    }

    public void Delete(int DataIndex) {
        if(DataIndex > MaxIndex || DataIndex <= 0) {
            Debug.LogWarning("DataIndexの設定値が正しくない");
        } else {
            saveDataRecorder.Delete(DataIndex - 1);
        }
    }

    //キャッシュを捨てる
    public void ClearCacheData() {
        cacheUsingData = null;

        cacheUsingData = new T();

        cacheUsingData.DeepCopy(usingData);
    }

    //以下は必要に応じて実装する
    protected abstract void ApplyCacheDataToData();

    protected abstract void OnSaveStartEvent();

    protected abstract void OnSaveCompletedEvent(string ErrMsg);

    protected abstract void OnLoadStartEvent();

    protected abstract void OnLoadCompletedEvent(bool LoadSuccessed);

    protected abstract void OnDeleteCompletedEvent(string ErrMsg);

    private void OnDestroy() {
        saveDataRecorder = null;
    }

#if UNITY_EDITOR
    private void Start() {
        Init();
    }
#endif
}