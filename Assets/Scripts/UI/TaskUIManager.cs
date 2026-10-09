using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class TaskUIManager : MonoBehaviour
{
    [Header("任务UI")]
    public Button btnTask;
    public TextMeshProUGUI taskText;

    // 任务数据类：存储任务ID和描述
    [System.Serializable]
    public class TaskItem
    {
        public int taskId;
        public string description;
    }

    private List<TaskItem> taskList = new List<TaskItem>();

    private void Start()
    {
        btnTask.onClick.AddListener(ToggleTaskPanel);

        //测试使用，添加任务
        
        AddTask(1, "收集到武器(0/3)");
        AddTask(2, "收集到物品(0/1)");
        AddTask(3, "观测到怪异");
        

        RefreshTaskText();
        gameObject.SetActive(false);
    }

    void ToggleTaskPanel()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    void RefreshTaskText()
    {
        string content = "";
        for (int i = 0; i < taskList.Count; i++)
        {
            var task = taskList[i];
            content += $"{task.taskId}. {task.description}\n";
        }
        taskText.text = content;
    }

    #region 对外接口
    /// <summary>
    /// 添加任务，传入任务ID和任务描述
    /// </summary>
    /// <param name="id">任务唯一整数ID</param>
    /// <param name="desc">任务文本描述</param>
    public void AddTask(int id, string desc)
    {
        // 检查ID是否已存在，避免重复任务
        foreach (var t in taskList)
        {
            if (t.taskId == id)
            {
                Debug.LogWarning($"任务ID {id} 已存在，跳过添加");
                return;
            }
        }

        TaskItem newTask = new TaskItem();
        newTask.taskId = id;
        newTask.description = desc;
        taskList.Add(newTask);
        RefreshTaskText();
    }

    /// <summary>
    /// 根据任务ID移除任务
    /// </summary>
    /// <param name="id">要移除的任务ID</param>
    public void RemoveTask(int id)
    {
        TaskItem target = null;
        foreach (var t in taskList)
        {
            if (t.taskId == id)
            {
                target = t;
                break;
            }
        }

        if (target != null)
        {
            taskList.Remove(target);
            RefreshTaskText();
        }
        else
        {
            Debug.LogWarning($"未找到ID为 {id} 的任务，移除失败");
        }
    }

    /// <summary>
    /// 移除所有任务
    /// </summary>

    public void ClearAllTask()
    {
        taskList.Clear();
        RefreshTaskText();
    }
    #endregion
}
