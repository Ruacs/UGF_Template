using GameFramework.DataTable;
using Lokas;
using System.Collections.Generic;
using UnityEngine;
using DataRowBase = UnityGameFramework.Runtime.DataRowBase;

public class NameProvider
{
    private List<int> _shuffledIds;
    private int _currentIndex = 0;

    public NameProvider()
    {
        Init<DRNameDataEN>();

        return;
        // if (GameEntry.Localization.Language == GameFramework.Localization.Language.English)


        // else
        //             Init<DRNameDataCN>();
    }

    private void Init<T>() where T : DataRowBase
    {
        IDataTable<T> dt = GameEntry.DataTable.GetDataTable<T>();
        _shuffledIds = new List<int>();

        foreach (var row in dt)
            _shuffledIds.Add(row.Id);

        Shuffle(_shuffledIds);
        _currentIndex = 0;
    }

    public string GetRandomName()
    {
        if (_shuffledIds == null || _shuffledIds.Count == 0)
            return "KK";

        if (_currentIndex >= _shuffledIds.Count)
        {
            Shuffle(_shuffledIds);
            _currentIndex = 0;
        }

        int id = _shuffledIds[_currentIndex++];
        return GetNameById(id);
    }

    private void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private T GetNameDataById<T>(int nameId) where T : DataRowBase
    {
        IDataTable<T> dtNameData = GameEntry.DataTable.GetDataTable<T>();
        return dtNameData.GetDataRow(nameId);
    }

    public string GetNameById(int id)
    {

        var dr = GetNameDataById<DRNameDataEN>(id);
        return dr != null ? dr.Name : "KK";

        // if (GameEntry.Localization.Language == GameFramework.Localization.Language.English)
        // {

        // }
        // else
        // {
        //     var dr = GetNameDataById<DRNameDataCN>(id);
        //     return dr != null ? dr.Name : "KK";
        // }
    }
}
