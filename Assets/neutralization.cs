using UnityEngine;
using KModkit;
using System.Linq;
using System.Text.RegularExpressions;

public class Neutralization : MonoBehaviour {

    public KMAudio Audio;
    public KMBombInfo Info;
    public KMBombModule Module;
    public KMSelectable[] btn;
    public MeshRenderer liquid, filterBtn;
    public GameObject liquidControl, colorText;
    public TextMesh[] Text;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    private readonly string[] _acidForm = { "HF", "HCl", "HBr", "HI" }, _baseForm = { "NH3", "LiOH", "NaOH", "KOH" }, _dispForm = { "NH\u2083", "LiOH", "NaOH", "KOH" }, clrName = { "Yellow", "Green", "Red", "Blue" };
    private int _selectBase = 0, _selectVol = 0, acidVol, acidType, acidConc, baseVol, baseType, baseConc;
    private bool filterMode = false, soluType, _isSolved = false, _lightsOn = false;
    private readonly bool[,] solubility = new bool[4, 4] {
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
             BaseTypeAdjust(0);
             return false;
         };
        btn[1].OnInteract += delegate ()
        {
            BaseTypeAdjust(1);
            return false;
        };
        btn[2].OnInteract += delegate ()
        {
            BaseConcAdjust(0);
            return false;
        };
        btn[3].OnInteract += delegate ()
        {
            BaseConcAdjust(1);
            return false;
        };
        btn[4].OnInteract += delegate ()
        {
            BaseConcAdjust(2);
            return false;
        };
        btn[5].OnInteract += delegate ()
        {
            BaseConcAdjust(3);
            return false;
        };
        btn[6].OnInteract += delegate ()
        {
            FilterModeTog();
            return false;
        };
        btn[7].OnInteract += delegate ()
        {
            AnsChk();
            return false;
        };
    }

    void Init()
    {
        Debug.LogFormat("[Neutralization #{0}] Begin detailed calculation report:", _moduleId);
        Debug.LogFormat("[Neutralization #{0}] Note: 'anion' = Acid's anion and 'cation' = Base's cation.", _moduleId);
        PrepareAcid();
        PrepareBase();
        PrepareConc();
        Debug.LogFormat("[Neutralization #{0}] End detailed calculation report.", _moduleId);

        Debug.LogFormat("[Neutralization #{0}] Acid color: {1}, Acid type: {2}, Acid vol: {3}, Acid conc: {4}", _moduleId, clrName[acidType], _acidForm[acidType], acidVol, acidConc / 10f);
        Debug.LogFormat("[Neutralization #{0}] Base type: {1}, Base conc: {2}", _moduleId, _baseForm[baseType], baseConc);
        Debug.LogFormat("[Neutralization #{0}] Drop count: {1}, Filter enable: {2}", _moduleId, baseVol, soluType);

        _lightsOn = true;
    }

    void PrepareAcid()
    {
        acidType = Random.Range(0, 4);
        acidVol = Random.Range(1, 5) * 5;

        Debug.LogFormat("[Neutralization #{0}] >Report A: Acid preparation", _moduleId);
        Debug.LogFormat("[Neutralization #{0}] A:\\>Acid type is {1} which has {2} color and volume of {3}.", _moduleId, _acidForm[acidType], clrName[acidType], acidVol);

        if (acidType == 0) liquid.GetComponent<MeshRenderer>().material.color = Color.yellow;
        else if (acidType == 1) liquid.GetComponent<MeshRenderer>().material.color = Color.green;
        else if (acidType == 2) liquid.GetComponent<MeshRenderer>().material.color = Color.red;
        else liquid.GetComponent<MeshRenderer>().material.color = Color.blue;

        //colorblind check
        if (GetComponent<KMColorblindMode>().ColorblindModeActive)
            EnableHelperText(acidType);

        if(acidVol == 5) liquidControl.gameObject.transform.localScale = new Vector3(22.22222f, 50, 1.43f);
        else if(acidVol == 10) liquidControl.gameObject.transform.localScale = new Vector3(22.22222f, 50, 3.73f);
        else if(acidVol == 15) liquidControl.gameObject.transform.localScale = new Vector3(22.22222f, 50, 6.12f);
        else liquidControl.gameObject.transform.localScale = new Vector3(22.22222f, 50, 8.44f);
    }

    void PrepareBase()
    {
        string temp = string.Join("", Info.GetIndicators().ToArray());

        Debug.LogFormat("[Neutralization #{0}] >Report B: Base preparation", _moduleId);

        if (Info.IsIndicatorPresent(Indicator.NSA) && Info.GetBatteryCount() == 3)
        {
            baseType = 0;
            Debug.LogFormat("[Neutralization #{0}] B:\\>NSA and 3 batt: {1}", _moduleId, _baseForm[baseType]);
        }
        else if (Info.IsIndicatorOn(Indicator.CAR) || Info.IsIndicatorOn(Indicator.FRQ) || Info.IsIndicatorOn(Indicator.IND))
        {
            baseType = 3;
            Debug.LogFormat("[Neutralization #{0}] B:\\>CAR, FRQ, or IND: {1}", _moduleId, _baseForm[baseType]);
        }
        else if (Info.GetPortCount() == 0 && Info.GetSerialNumberLetters().Any("AEIOU".Contains))
        {
            baseType = 1;
            Debug.LogFormat("[Neutralization #{0}] B:\\>No ports and vowel in S/N: {1}", _moduleId, _baseForm[baseType]);
        }
        else if (temp.Any(_acidForm[acidType].ToUpper().Contains))
        {
            baseType = 3;
            Debug.LogFormat("[Neutralization #{0}] B:\\>Acid formular has letter in common with an indicator: {1}", _moduleId, _baseForm[baseType]);
        }
        else if (Info.GetBatteryCount(Battery.D) > Info.GetBatteryCount(Battery.AA))
        {
            baseType = 0;
            Debug.LogFormat("[Neutralization #{0}] B:\\>D batt > AA batt: {1}", _moduleId, _baseForm[baseType]);
        }
        else if (acidType == 0 || acidType == 1)
        {
            baseType = 2;
            Debug.LogFormat("[Neutralization #{0}] B:\\>Anion < 20: {1}", _moduleId, _baseForm[baseType]);
        }
        else
        {
            baseType = 1;
            Debug.LogFormat("[Neutralization #{0}] B:\\>Otherwise: {1}", _moduleId, _baseForm[baseType]);
        }
    }

    void PrepareConc()
    {
        int[] anion = { 9, 17, 35, 53 }, cation = { 1, 3, 11, 19 }, len = { 1, 2, 2, 1 };
        int bh = Info.GetBatteryHolderCount(), port = Info.GetPorts().Distinct().Count(), indc = Info.GetIndicators().Count();

        Debug.LogFormat("[Neutralization #{0}] >Report C: Concentration calculation", _moduleId);

        //acid
        acidConc = anion[acidType];
        Debug.LogFormat("[Neutralization #{0}] C:\\acid>Starts with anion: {1} [current = {2}]", _moduleId, anion[acidType], acidConc);
        acidConc -= cation[baseType];
        Debug.LogFormat("[Neutralization #{0}] C:\\acid>Sub with cation: -{1} [current = {2}]", _moduleId, cation[baseType], acidConc);
        if (acidType == 3 || baseType == 1 || baseType == 2)
        {
            acidConc -= 4;
            Debug.LogFormat("[Neutralization #{0}] C:\\acid>Anion/Cation contains vowels: -4 [current = {1}]", _moduleId, acidConc);
        }
        if (len[acidType] == len[baseType])
        {
            acidConc *= 3;
            Debug.LogFormat("[Neutralization #{0}] C:\\acid>Length of anion = cation: x3 [current = {1}]", _moduleId, acidConc);
        }
        if (acidConc <= 0)
        {
            acidConc *= -1;
            Debug.LogFormat("[Neutralization #{0}] C:\\acid>Negative adjust [current = {1}]", _moduleId, acidConc);
        }
        acidConc %= 10;
        Debug.LogFormat("[Neutralization #{0}] C:\\acid>Take LSD: {1}", _moduleId, acidConc);
        if (acidConc == 0)
        {
            acidConc = (acidVol * 2) / 5;
            Debug.LogFormat("[Neutralization #{0}] C:\\acid>LSD is 0, use (Acid vol x2)/5: {1}", _moduleId, acidConc);
        }
        Debug.LogFormat("[Neutralization #{0}] C:\\acid>Final acid conc = {1}", _moduleId, acidConc / 10f);

        //base
        if ((acidType == 3 && baseType == 3) || (acidType == 1 && baseType == 0))
        {
            baseConc = 20;
            Debug.LogFormat("[Neutralization #{0}] C:\\base>Special pair, use constant conc. 20", _moduleId);
        }
        else if (bh > port && bh > indc)
        {
            baseConc = 5;
            Debug.LogFormat("[Neutralization #{0}] C:\\base>Battery holders win, use conc. 5", _moduleId);
        }
        else if (port > bh && port > indc)
        {
            baseConc = 10;
            Debug.LogFormat("[Neutralization #{0}] C:\\base>Port types win, use conc. 10", _moduleId);
        }
        else if (indc > bh && indc > port)
        {
            baseConc = 20;
            Debug.LogFormat("[Neutralization #{0}] C:\\base>Indicators win, use conc. 20", _moduleId);
        }
        else if (baseType == 2)
        {
            baseConc = 10;
            Debug.LogFormat("[Neutralization #{0}] C:\\base>Tie, use 10 because it's closest to 11", _moduleId);
        }
        else if (baseType == 3)
        {
            baseConc = 20;
            Debug.LogFormat("[Neutralization #{0}] C:\\base>Tie, use 20 because it's closest to 19", _moduleId);
        }
        else
        {
            baseConc = 5;
            Debug.LogFormat("[Neutralization #{0}] C:\\base>Tie, use 5 because it's closest to 1 or 3", _moduleId);
        }

        //drop cnt & solu
        baseVol = 20 / baseConc;
        Debug.LogFormat("[Neutralization #{0}] C:\\drop>Starts with 20 / Base conc. {1} = {2}", _moduleId, baseConc, baseVol);
        baseVol *= (acidVol * acidConc) / 10;
        Debug.LogFormat("[Neutralization #{0}] C:\\drop>Then mult with Acid vol {1} and Acid conc. {2} = {3}", _moduleId, acidVol, acidConc / 10f, baseVol);
        Debug.LogFormat("[Neutralization #{0}] C:\\drop>Final drop count is {1}", _moduleId, baseVol);
        soluType = solubility[acidType, baseType];
        if(soluType == true) Debug.LogFormat("[Neutralization #{0}] C:\\solu>Pair of {1} and {2} is not soluble, turn filter on.", _moduleId, _acidForm[acidType], _baseForm[baseType]);
        else Debug.LogFormat("[Neutralization #{0}] C:\\solu>Pair of {1} and {2} is soluble, turn filter off.", _moduleId, _acidForm[acidType], _baseForm[baseType]);
    }

    void EnableHelperText(int acidType)
    {
        if (acidType == 0) colorText.GetComponent<TextMesh>().text = "Yellow";
        else if (acidType == 1) colorText.GetComponent<TextMesh>().text = "Green";
        else if (acidType == 2) colorText.GetComponent<TextMesh>().text = "Red";
        else colorText.GetComponent<TextMesh>().text = "Blue";

        Debug.LogFormat("[Neutralization #{0}] A:\\>Colorblind mode enabled, showing color of acid in text.", _moduleId);
        colorText.SetActive(true);
    }

    void BaseTypeAdjust(int m)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[m].transform);
        btn[m].AddInteractionPunch();
        if (!_lightsOn || _isSolved) return;

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
        Text[0].text = _dispForm[_selectBase];
    }

    void BaseConcAdjust(int m)
    {
        string temp = "";

        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[m + 2].transform);
        btn[m + 2].AddInteractionPunch();
        if (!_lightsOn || _isSolved) return;

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

    void FilterModeTog()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[6].transform);
        btn[6].AddInteractionPunch();
        if (!_lightsOn || _isSolved) return;

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

    void AnsChk()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[7].transform);
        btn[7].AddInteractionPunch();
        if (!_lightsOn || _isSolved) return;

        Debug.LogFormat("[Neutralization #{0}] Answered: Base type = {1} (Expected {2})", _moduleId, _baseForm[_selectBase], _baseForm[baseType]);
        Debug.LogFormat("[Neutralization #{0}] Answered: Drop count = {1} (Expected {2})", _moduleId, _selectVol, baseVol);
        Debug.LogFormat("[Neutralization #{0}] Answered: Filter enable = {1} (Expected {2})", _moduleId, filterMode, soluType);

        if (_selectBase == baseType && _selectVol == baseVol && filterMode == soluType)
        {
            liquid.GetComponent<MeshRenderer>().material.color = Color.white;
            Debug.LogFormat("[Neutralization #{0}] Answer correct! Module passed!", _moduleId);
            Audio.PlaySoundAtTransform("correct", Module.transform);
            Module.HandlePass();
            _isSolved = true;
        }
        else
        {
            Debug.LogFormat("[Neutralization #{0}] Answer incorrect! Strike!", _moduleId);
            Audio.PlaySoundAtTransform("strike", Module.transform);
            Module.HandleStrike();
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Set the base with “!{0} base prev 1” (press the < button 1 time) or “!{0} base next 2” (press the > button 2 times) or “!{0} base NaOH” (direct).
                                                    Set the concentration to 55 with “!{0} conc set 55”. Add or subtract the concentration with “!{0} conc var (num, negative allowed)”.
                                                    Toggle the filter with “!{0} filter”. Submit the answer with “!{0} titrate”.";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        int temp;
        KMSelectable[] cmdA, cmdB;

        command = command.ToLowerInvariant().Trim();

        if (command.Equals("base prev")) return new[] { btn[0] };
        else if (command.Equals("base next")) return new[] { btn[1] };
        else if (command.Equals("filter") || command.Equals("toggle")) return new[] { btn[6] };
        else if (command.Equals("titrate") || command.Equals("submit")) return new[] { btn[7] };

        if (Regex.IsMatch(command, @"^base [a-z0-9]+$"))
        {
            command = command.Substring(5);
            int index = System.Array.FindIndex(_baseForm, p => p.Equals(command, System.StringComparison.InvariantCultureIgnoreCase));
            if ( (index > -1) && (index != _selectBase) )
            {
                int distance = index - _selectBase;
                switch (distance)
                {
                    case -1:
                    case 3:
                        return new[] { btn[0] };
                    case -2:
                    case 2:
                        return new[] { btn[1], btn[1] };
                    case -3:
                    case 1:
                        return new[] { btn[1] };
                };
            }
        }

        if (Regex.IsMatch(command, @"^conc set \d\d?$"))
        {
            command = command.Substring(9);
            temp = int.Parse(command) - _selectVol;
            if (temp > -10 && temp < 0) command = "conc var -0" + (temp * -1).ToString();
            else if (temp > -1 && temp < 10) command = "conc var 0" + temp.ToString();
            else command = "conc var " + temp.ToString();
        }

        if(Regex.IsMatch(command, @"^conc var \d\d?$"))
        {
            command = command.Substring(9);
            temp = int.Parse(command);
            cmdA = Enumerable.Repeat(btn[2], temp / 10).ToArray();
            cmdB = Enumerable.Repeat(btn[3], temp % 10).ToArray();
            return cmdA.Concat(cmdB).ToArray();
        }

        if(Regex.IsMatch(command, @"^conc var -\d\d?$"))
        {
            command = command.Substring(10);
            temp = int.Parse(command);
            cmdA = Enumerable.Repeat(btn[4], temp / 10).ToArray();
            cmdB = Enumerable.Repeat(btn[5], temp % 10).ToArray();
            return cmdA.Concat(cmdB).ToArray();
        }

        return null;
    }
}
