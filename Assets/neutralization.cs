using UnityEngine;
using KMHelper;
using System.Linq;

public class neutralization : MonoBehaviour {

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    public KMAudio Audio;
    public KMBombInfo Info;
    public KMBombModule Module;
    public KMSelectable[] btn;
    public MeshRenderer liquid, filterBtn;
    public GameObject liquidControl;
    public TextMesh[] Text;

    private string[] _acidForm = { "HF", "HCl", "HBr", "HI" }, _baseForm = { "NH3", "LiOH", "NaOH", "KOH" };
    private int _selectBase = 0, _selectVol = 0, acidVol, acidType, acidConc, baseVol, baseType, baseConc;
    private bool filterMode = false, soluType;
    private bool[,] solubility = new bool[4, 4] {
        {true,true,false,false},
        {true,false,true,true},
        {false,true,false,true},
        {false,false,true,false},
    };

    void Start () {
        _moduleId = _moduleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Init;
    }

    private void Awake()
    {
        btn[0].OnInteract += delegate ()
         {
             baseTypeAdjust(0);
             return false;
         };
        btn[1].OnInteract += delegate ()
        {
            baseTypeAdjust(1);
            return false;
        };
        btn[2].OnInteract += delegate ()
        {
            baseConcAdjust(0);
            return false;
        };
        btn[3].OnInteract += delegate ()
        {
            baseConcAdjust(1);
            return false;
        };
        btn[4].OnInteract += delegate ()
        {
            baseConcAdjust(2);
            return false;
        };
        btn[5].OnInteract += delegate ()
        {
            baseConcAdjust(3);
            return false;
        };
        btn[6].OnInteract += delegate ()
        {
            filterModeTog();
            return false;
        };
        btn[7].OnInteract += delegate ()
        {
            ansChk();
            return false;
        };
    }

    void Init()
    {
        string[] clrName = { "Yellow", "Green", "Red", "Blue" };

        prepareAcid();
        prepareBase();
        prepareConc();

        baseVol = 20 / baseConc;
        baseVol *= (acidVol * acidConc) / 10;
        soluType = solubility[acidType,baseType];

        Debug.LogFormat("[Neutralization #{0}] Acid color: {1}, Acid type: {2}, Acid vol: {3}, Acid conc: {4}", _moduleId, clrName[acidType],_acidForm[acidType], acidVol, acidConc / 10f);
        Debug.LogFormat("[Neutralization #{0}] Base type: {1}, Base conc: {2}", _moduleId, _baseForm[baseType], baseConc);
        Debug.LogFormat("[Neutralization #{0}] Drop count: {1}, Filter enable: {2}", _moduleId, baseVol, soluType);
    }

    void prepareAcid()
    {
        acidType = Random.Range(0, 4);
        acidVol = Random.Range(1, 5) * 5;

        if (acidType == 0) liquid.GetComponent<MeshRenderer>().material.color = Color.yellow;
        else if (acidType == 1) liquid.GetComponent<MeshRenderer>().material.color = Color.green;
        else if (acidType == 2) liquid.GetComponent<MeshRenderer>().material.color = Color.red;
        else liquid.GetComponent<MeshRenderer>().material.color = Color.blue;

        if(acidVol == 5) liquidControl.gameObject.transform.localScale = new Vector3(22.22222f, 50, 1.43f);
        else if(acidVol == 10) liquidControl.gameObject.transform.localScale = new Vector3(22.22222f, 50, 3.73f);
        else if(acidVol == 15) liquidControl.gameObject.transform.localScale = new Vector3(22.22222f, 50, 6.12f);
        else liquidControl.gameObject.transform.localScale = new Vector3(22.22222f, 50, 8.44f);
    }

    void prepareBase()
    {
        string temp = string.Join("", Info.GetIndicators().ToArray());

        if (Info.IsIndicatorPresent(KMBombInfoExtensions.KnownIndicatorLabel.NSA) && Info.GetBatteryCount() == 3) baseType = 0;
        else if (Info.IsIndicatorOn(KMBombInfoExtensions.KnownIndicatorLabel.CAR) || Info.IsIndicatorOn(KMBombInfoExtensions.KnownIndicatorLabel.FRQ) || Info.IsIndicatorOn(KMBombInfoExtensions.KnownIndicatorLabel.IND)) baseType = 3;
        else if (Info.GetPortCount() == 0 && Info.GetSerialNumberLetters().Any("AEIOU".Contains)) baseType = 1;
        else if (temp.Any(_acidForm[acidType].ToUpper().Contains)) baseType = 3;
        else if (Info.GetBatteryCount(KMBombInfoExtensions.KnownBatteryType.D) > Info.GetBatteryCount(KMBombInfoExtensions.KnownBatteryType.AA)) baseType = 0;
        else if (acidType == 0 || acidType == 1) baseType = 2;
        else baseType = 1;
    }

    void prepareConc()
    {
        int[] anion = { 9, 17, 35, 53 }, cation = { 1, 3, 11, 19 };
        bool[] acidV = { false, false, false, true }, baseV = { false, true, true, false };
        int bh = Info.GetBatteryHolderCount(), port = Info.GetPorts().Distinct().Count(), indc = Info.GetIndicators().Count();

        acidConc = anion[acidType];
        if (acidV[acidType] || baseV[baseType]) acidConc *= 2;
        acidConc += (acidVol / 5) - cation[baseType];
        acidConc %= 10;
        if (acidConc % 2 == 1) acidConc--;
        if (acidConc == 0) acidConc = 6;

        if ((acidType == 3 && baseType == 3) || (acidType == 1 && baseType == 0)) baseConc = 20;
        else if (bh > port && bh > indc) baseConc = 5;
        else if (port > bh && port > indc) baseConc = 10;
        else if (indc > bh && indc > port) baseConc = 20;
        else baseConc = 5;
    }

    void baseTypeAdjust(int m)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[m].transform);
        btn[m].AddInteractionPunch();

        if (m == 0)
        {
            _selectBase--;
            if (_selectBase < 0) _selectBase = 3;
        }
        else
        {
            _selectBase++;
            if (_selectBase > 3) _selectBase = 0;
        }
        Text[0].text = _baseForm[_selectBase];
    }

    void baseConcAdjust(int m)
    {
        string temp = "";

        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[m + 2].transform);
        btn[m + 2].AddInteractionPunch();

        if (m == 0) _selectVol += 10;
        else if (m == 1) _selectVol++;
        else if (m == 2) _selectVol -= 10;
        else _selectVol--;

        if (_selectVol > 99) _selectVol = 99;
        else if (_selectVol < 0) _selectVol = 0;

        if (_selectVol > 9) temp = _selectVol.ToString();
        else temp = "0" + _selectVol.ToString();
        Text[1].text = temp;
    }

    void filterModeTog()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[6].transform);
        btn[6].AddInteractionPunch();

        if (filterMode == false)
        {
            filterMode = true;
            filterBtn.GetComponent<MeshRenderer>().material.color = Color.green;
            Text[2].text = "ON";
        }
        else
        {
            filterMode = false;
            filterBtn.GetComponent<MeshRenderer>().material.color = Color.red;
            Text[2].text = "OFF";
        }
    }

    void ansChk()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[7].transform);
        btn[7].AddInteractionPunch();

        Debug.LogFormat("[Neutralization #{0}] Answered: Base type = {1} (Expected {2})", _moduleId, _baseForm[_selectBase], _baseForm[baseType]);
        Debug.LogFormat("[Neutralization #{0}] Answered: Drop count = {1} (Expected {2})", _moduleId, _selectVol, baseVol);
        Debug.LogFormat("[Neutralization #{0}] Answered: Filter enable = {1} (Expected {2})", _moduleId, filterMode, soluType);

        if (_selectBase == baseType && _selectVol == baseVol && filterMode == soluType)
        {
            liquid.GetComponent<MeshRenderer>().material.color = Color.white;
            Debug.LogFormat("[Neutralization #{0}] Answer correct! Module passed!", _moduleId);
            Audio.PlaySoundAtTransform("correct", Module.transform);
            Module.HandlePass();
        }
        else
        {
            Debug.LogFormat("[Neutralization #{0}] Answer incorrect! Strike!", _moduleId);
            Audio.PlaySoundAtTransform("strike", Module.transform);
            Module.HandleStrike();
        }
    }
}
