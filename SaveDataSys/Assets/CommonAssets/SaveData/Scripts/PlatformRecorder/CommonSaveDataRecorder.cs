using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.IO;
using UniRx;
using GCustomSaveData;

//実際のデータを扱うクラス、必要に応じてプラットフォームごとに実装
//とりあえずこのクラスはPCとスマホを対応する、コンシューマー機器の場合「ISaveDataRecorder」を継承し実装してください
sealed public class CommonSaveDataRecorder<T> : ISaveDataRecorder<T> where T : BaseSaveData {
    //セーブデータファイル名
    private const string BaseFileName = "SaveData";

    private Dictionary<int, string> pathDict;

    private Dictionary<int, string> bkPathDict;

    private int saveDataMaxIndex = 1;

    private bool backUpOn = true;

    private SaveDataType saveDataType;

    private Subject<T> onLoadCompleted;

    private Subject<string> onSaveCompleted;

    private Subject<string> onDeleteCompleted;

    public bool IsNothing {
        get {
            return !AnySaveDataExists();
        }
    }

    public IObservable<T> OnLoadCompleted() {
        return onLoadCompleted ?? (onLoadCompleted = new Subject<T>());
    }

    public IObservable<string> OnSaveCompleted() {
        return onSaveCompleted ?? (onSaveCompleted = new Subject<string>());
    }

    public IObservable<string> OnDeleteCompleted() {
        return onDeleteCompleted ?? (onDeleteCompleted = new Subject<string>());
    }

    public void Init(int MaxIndex, SaveDataType DataType, string ProductName, bool BackUpOn = true) {
        saveDataMaxIndex = MaxIndex;

        saveDataType = DataType;

        var filePath = $"{Application.persistentDataPath}/{ProductName}/";

        //ディレクトリー存在しなければ、作成する
        if(!string.IsNullOrEmpty(filePath) && !Directory.Exists(filePath)) {
            Directory.CreateDirectory(filePath);
        }

        backUpOn = BackUpOn;

        pathDict = new Dictionary<int, string>();

        bkPathDict = new Dictionary<int, string>();

        //ファイルパスをセット
        for(int i = 0;i < MaxIndex;i++) {
            var path = DataType == SaveDataType.Binary ? $"{filePath}{BaseFileName}_{i}" : $"{filePath}{BaseFileName}_{i}.txt";

            var bkPath = DataType == SaveDataType.Binary ? $"{filePath}{BaseFileName}_{i}_BackUp" : $"{filePath}{BaseFileName}_{i}_BackUp.txt";

            pathDict.Add(i, path);

            bkPathDict.Add(i, bkPath);
        }
    }

    //NOTE:実際に上書きする際に一回上書きする確認を取らせる必要があります
    //NOTE:セーブする際に必ず予備のデータを先に書き込む
    public void Save(int DataIndex, T SaveDataIn) {
        if(saveDataType == SaveDataType.Json) {
            SaveJson(DataIndex, SaveDataIn);
        } else {
            SaveBinary(DataIndex, SaveDataIn);
        }
    }

    //ロードではまず正式データから試す、もしロード失敗したら、予備データをロード、さらに失敗したら
    //データなしとみなす
    public void Load(int DataIndex) {
        if(saveDataType == SaveDataType.Json) {
            LoadJson(DataIndex);
        } else {
            LoadBinary(DataIndex);
        }
    }

    //データファイル削除
    public void Delete(int DataIndex) {
        var path = pathDict.ContainsKey(DataIndex) ? pathDict[DataIndex] : string.Empty;

        string errMsg = string.Empty;

        try {
            if(File.Exists(path)) {
                File.Delete(path);
            }

            //バックアップデータがあれば、一緒に削除
            if(File.Exists(bkPathDict[DataIndex])) {
                File.Delete(bkPathDict[DataIndex]);
            }
        } catch(System.Exception err) {
            errMsg = err.ToString();

            Debug.LogError($"ファイルデータ削除失敗メッセージ: {errMsg}");
        }

        OnDeletedEvent(errMsg);
    }

    private void SaveJson(int DataIndex, T SaveDataIn) {
        var data = JsonUtility.ToJson(SaveDataIn);

        var path = pathDict.ContainsKey(DataIndex) ? pathDict[DataIndex] : string.Empty;

        string errMsg = string.Empty;

        try {
            //新規セーブする前にバックアップを取る
            if(backUpOn) {
                if(File.Exists(path))
                    File.Copy(path, bkPathDict[DataIndex], true);
            }

            File.WriteAllText(path, data, System.Text.Encoding.UTF8);
        } catch(System.Exception err) {
            errMsg = err.ToString();

            Debug.LogError($"ファイルデータセーブ失敗メッセージ: {errMsg}");
        }

        OnSaveCompletedEvent(errMsg);
    }

    private void LoadJson(int DataIndex) {
        var path = pathDict.ContainsKey(DataIndex) ? pathDict[DataIndex] : string.Empty;

        T saveData = null;

        if(File.Exists(path)) {
            try {
                //本番データロード
                var str = File.ReadAllText(path);

                saveData = JsonUtility.FromJson<T>(str);
            } catch(System.Exception err) {
                Debug.LogError($"ファイルデータロード失敗メッセージ: {err.ToString()}");

                //本番でうまく読み取れない場合バックアップを使用してみる
                if(backUpOn) {
                    var str = File.ReadAllText(bkPathDict[DataIndex], System.Text.Encoding.UTF8);

                    try {
                        saveData = JsonUtility.FromJson<T>(str);
                    } catch(System.Exception bkErr) {
                        Debug.LogError($"ファイルデータロード失敗メッセージ: {bkErr.ToString()}");
                    }
                }
            }
        }

        OnLoadCompletedEvent(saveData);
    }

    private void SaveBinary(int DataIndex, T SaveDataIn) {
        var data = (object)SaveDataIn;

        var path = pathDict.ContainsKey(DataIndex) ? pathDict[DataIndex] : string.Empty;

        string errMsg = string.Empty;

        BinaryFormatter bf = new BinaryFormatter();

        try {
            //新規セーブする前にバックアップを取る
            if(backUpOn) {
                if(File.Exists(path))
                    File.Copy(path, bkPathDict[DataIndex], true);
            }

            //バイナリに変換する
            using(var sw = new MemoryStream()) {
                bf.Serialize(sw, data);

                //ファイルを書き込む
                File.WriteAllBytes(path, sw.ToArray());
            }
        } catch(System.Exception err) {
            errMsg = err.ToString();

            Debug.LogError($"ファイルデータセーブ失敗メッセージ: {errMsg}");
        }

        OnSaveCompletedEvent(errMsg);
    }

    private void LoadBinary(int DataIndex) {
        var path = pathDict.ContainsKey(DataIndex) ? pathDict[DataIndex] : string.Empty;

        T saveData = null;

        if(File.Exists(path)) {
            BinaryFormatter bf = new BinaryFormatter();

            try {
                using(var fr = File.Open(path, FileMode.Open)) {
                    saveData = bf.Deserialize(fr) as T;
                }
            } catch(System.Exception err) {
                Debug.LogError($"ファイルデータロード失敗メッセージ: {err.ToString()}");

                if(backUpOn) {
                    try {
                        using(var fr = File.Open(bkPathDict[DataIndex], FileMode.Open)) {
                            saveData = bf.Deserialize(fr) as T;
                        }
                    } catch(System.Exception bkErr) {
                        Debug.LogError($"ファイルデータロード失敗メッセージ: {bkErr.ToString()}");
                    }
                }
            }
        }

        OnLoadCompletedEvent(saveData);
    }

    //任意のセーブデータがあればtrue
    private bool AnySaveDataExists() {
        //任意のデータがあればtrue
        for(int i = 0;i < saveDataMaxIndex;i++) {
            if(File.Exists(pathDict[i])) {
                return true;
            }
        }

        return false;
    }

    private void OnLoadCompletedEvent(T Data) {
        onLoadCompleted?.OnNext(Data);
    }

    private void OnSaveCompletedEvent(string Msg) {
        onSaveCompleted?.OnNext(Msg);
    }

    private void OnDeletedEvent(string Msg) {
        onDeleteCompleted?.OnNext(Msg);
    }

    ~CommonSaveDataRecorder() {
        if(pathDict != null)
            pathDict.Clear();

        if(bkPathDict != null)
            bkPathDict.Clear();
    }
}
