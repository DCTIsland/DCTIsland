using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;
using System.Threading.Tasks;
using System.Collections.Generic;

[System.Serializable]
public class FirebaseDataThread
{
    public string emotion;
    public string image_url;
    public string link;
    public string thread_id;
    public string topic1;
    public string topic2;
    public string topic3;
}

public class FirebaseManager : MonoBehaviour
{
    public IslandManage islandManage;
    private int existingDataCount = 0;

    void Start()
    {
        // 初始化 Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("Firebase 初始化成功！");
                InitializeDatabase();
            }
            else
            {
                Debug.LogError($"Firebase 初始化失敗：{task.Result}");
            }
        });
    }

    void InitializeDatabase()
    {
        // 取得資料庫的根引用
        DatabaseReference databaseReference = FirebaseDatabase.DefaultInstance.GetReference("threads");

        //讀目前已存在的資料數
        databaseReference.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                existingDataCount = (int)task.Result.ChildrenCount;
                Debug.Log("Existing data count: " + existingDataCount);
            }

            // 監聽 exampleNode 資料的變化
            databaseReference.ChildAdded += HandleChildAdded;
        });

        Debug.Log("資料庫監聽已啟動！");
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs e)
    {
        //監聽錯誤
        if (e.DatabaseError != null)
        {
            Debug.LogError($"資料庫監聽錯誤：{e.DatabaseError.Message}");
            return;
        }

        //讀取已存在數據後不理他
        if(existingDataCount > 0){
            existingDataCount--;
            return;
        }

        // 確認有數據返回
        if (e.Snapshot != null && e.Snapshot.Value != null)
        {
            string key = e.Snapshot.Key; // 最新資料的 Key
            string value = e.Snapshot.GetRawJsonValue(); // 最新資料的內容

            Debug.Log($"最新資料:{key}");
            Debug.Log($"最新資料內容：{value}");

            FirebaseDataThread valueObj = JsonUtility.FromJson<FirebaseDataThread>(value);
            islandManage.AddToQueue(key, valueObj);
        }
        else
        {
            Debug.Log("資料庫中沒有數據。");
        }
    }

    public void UploadToStorage(byte[] pngData, string thread_id)
    {
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        StorageReference storageRef = storage.GetReferenceFromUrl("gs://dctdb-8c8ad.firebasestorage.app");
        StorageReference snapshotRef = storageRef.Child($"snapshots/{thread_id}.png");

        var pngMetadata = new MetadataChange();
        pngMetadata.ContentType = "image/png";

        snapshotRef.PutBytesAsync(pngData, pngMetadata).ContinueWith((Task<StorageMetadata> task) =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error to upload snapshot: " + task.Exception.ToString());
            }
            else
            {
                Debug.Log("Upload snapshot successful");
            }
        });
    }

    public void UpdateDB(string key, string thread_id)
    {
        DatabaseReference databaseReference = FirebaseDatabase.DefaultInstance.GetReference("threads");
        DatabaseReference updateRef = databaseReference.Child(key);

        Dictionary<string, object> data = new Dictionary<string, object>();
        data["snapshot_url"] = $"https://firebasestorage.googleapis.com/v0/b/dctdb-8c8ad.firebasestorage.app/o/snapshots%2F{thread_id}.png?alt=media";

        updateRef.UpdateChildrenAsync(data).ContinueWith(task =>
        {
            if (task.IsFaulted)
                Debug.LogError("Error update database: " + task.Exception);
            else
                Debug.Log("Update database Successful");
        });
    }
}
