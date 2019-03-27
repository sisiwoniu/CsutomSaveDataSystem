using UniRx;

namespace GCustomSaveData {

    //セーブデータを処理するインターフェース
    public interface ISaveDataRecorder<T> where T : BaseSaveData {

        void Init(int MaxIndex, SaveDataType DataType, string ProductName, bool BackUpOn = true);

        void Save(int DataIndex, T SaveDataIn);

        void Load(int DataIndex);

        void Delete(int DataIndex);

        IObservable<T> OnLoadCompleted();

        //空文字列の場合、セーブ成功と見なす
        IObservable<string> OnSaveCompleted();

        //空文字列の場合、削除成功と見なす
        IObservable<string> OnDeleteCompleted();

        //一個のデータもない場合true
        bool IsNothing {
            get;
        }
    }

    public enum SaveDataType {
        Json,
        Binary
    }

    //セーブデータのベース
    [System.Serializable]
    public abstract class BaseSaveData {
        public abstract void DeepCopy(BaseSaveData CopyTarget);
    }

#if UNITY_EDITOR

    [System.Serializable]
    public class DebugSaveData : BaseSaveData {
        //TODO:必要に応じてここにメンバーを入れて使ってください
        public int Num = 1;

        public string Name = "AAA";

        public int[] TestNum = { 1, 2, 3 };

        //参照型の場合、コピーのやり方を注意してください
        public override void DeepCopy(BaseSaveData CopyTarget) {
            var d = CopyTarget as DebugSaveData;

            Num = d.Num;

            Name = d.Name;

            TestNum = d.TestNum;
        }
    }

#endif
}
