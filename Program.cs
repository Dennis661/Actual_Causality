using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.RegularExpressions;

namespace Actual_Causality
{
    internal class Program
    {
        /*  examples of inputstrings:
                Z:=1 if X=1,Y=1;or; X=1,Y=0;or; X=0,Y=1; Z:=0 if X=0,Y=0;;
                Z:=11ifX=1,Y=1;or;X=1,Y=0;;OZD:=9999or;X=0,Y=1;Z:=0ifX=0,Y=0;;
                ST:= 1 if UST = 1; ST:= 0 if UST=0;;BT:= 1 if UBT = 1; BT:= 0 if UBT = 0;; SH:= 1 if ST = 1; SH:= 0 if ST = 0;;BH:= 1 if BT = 1,SH = 0; BH:= 0 if BT = 0,SH = 0; or; BT = 0,SH = 1; or; BT = 1,SH = 1;;BS:= 1 if SH = 1,BH = 1; or; SH = 1,BH = 0; or; SH = 0,BH = 1;  BS:= 0 if SH = 0,BH = 0; ;
         */
        struct thisVar  
        {
            public string name;
            public List<int> range;
            public List<string> domain;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome. This program is used for research on a good definition of Actual Causality. \nPlease provide the program with an input string.");
            string mainString = prepareInput();
            List<string> allVarNames = getAllVarNames(mainString);
            List<string> endoVarNames = getEndoVarNames(allVarNames, mainString);
            List<string> exoVarNames = getExoVarNames(allVarNames, endoVarNames, mainString);

            List<thisVar> vars = new List<thisVar>();
            foreach (string v in allVarNames)
            {
                thisVar currentVar = new thisVar();
                currentVar.name = v;
                currentVar.range = new List<int>();
                currentVar.domain = new List<string>();
                vars.Add(currentVar);
            }
            vars = getVarRanges(vars, allVarNames, mainString);
            vars = getVarDomains(vars, allVarNames, mainString);

            printPrerequisites(exoVarNames, endoVarNames, vars);
            Dictionary<string, int> exoValues = askExoValues(exoVarNames);

            Dictionary <string, int> values = calculateValues(exoValues, endoVarNames, vars, mainString);
        }
        static Dictionary<string, int> calculateValues(Dictionary<string, int> exoValues, List<string>endoNames, List<thisVar> vars, string mainstring)
        {
            Dictionary<string, int> known = exoValues;          // at first, only the exogenous variables are known
            List<string> unknown = endoNames;                   // leaves us with the endogenous variables which are unknown.
            Dictionary<string, int> tempKnown = new Dictionary<string, int>();      // this one starts off empty, as we use it to save temporary values.
            int iter = 0;

            while (unknown.Count > 0 && iter<6)           // we continue until there are no values unknown anymore, therefore we calculated all values
            {
                Console.WriteLine("This is iteration " + iter+".");
                Console.WriteLine("This is in unknown:");
                foreach (string s in unknown)
                {
                    Console.Write(s+", ");
                }
                Console.WriteLine();
                for(int i =0; i<unknown.Count; i++)         // for every variable in unknown
                {
                    Console.WriteLine("----------- We now go look for " + unknown[i] + ". ------------------");
                    int value = tryGetValue(unknown[i], known, vars, mainstring); // we try to calculate the value in this iter
                    if (value != int.MinValue)                                    // if it is not the default value of the tryGetValue function, meaning it got a value
                    {
                        tempKnown.Add(unknown[i], value);
                        Console.WriteLine("Added to tempknown!");
                        unknown.Remove(unknown[i]);
                        Console.WriteLine(unknown[i] + "  deleted from unknown.");
                    }
                }
                foreach(KeyValuePair<string, int> pair in tempKnown)
                {
                    known.Add(pair.Key, pair.Value);
                }
                tempKnown.Clear();
                iter ++;
            }
            return known;
        }
           // ST:=1ifUST=1;ST:=0ifUST=0   ;;BT:= 1 if UBT = 1; BT:= 0 if UBT = 0;; SH:= 1 if ST = 1; SH:= 0 if ST = 0;;BH:= 1 if BT = 1,SH = 0; BH:= 0 if BT = 0,SH = 0; or; BT = 0,SH = 1; or; BT = 1,SH = 1;;    BS:= 1 if SH = 1,BH = 1; or; SH = 1,BH = 0; or; SH = 0,BH = 1;  BS:= 0 if SH = 0,BH = 0; ;

        static int tryGetValue(string varName, Dictionary<string, int>known, List<thisVar>vars, string mainstring)
        {
            int value = int.MinValue;                               // default is the minimal int value, in case we cannot calculate any other value
            List<string>lookingFor = new List<string>();

            foreach (thisVar v in vars)         // search through all vars
            {
                if (v.name== varName)           // get the one with the same name as our varname
                {
                    foreach(string d in v.domain)   // check for all vars in the domain
                    {
                        foreach(KeyValuePair<string, int> kvp in known) // check for the intersection with the variables in the Known list
                        {
                            if (d==kvp.Key)               // if the names are the same, it means the variable is both known and in the domain
                            {
                                Console.WriteLine("Intersection between domain and known found, namely: " + d);
                                lookingFor.Add(kvp.Key + "=" + kvp.Value);
                                Console.WriteLine(kvp.Key + "=" + kvp.Value + " added to lookingFor!");
                            }
                        }
                    }
                }
            }
            string target = varName + ":=";
            string mainModified = mainstring.Replace(";;", ";");
            string[] split = mainModified.Split(';');
            int targetValue = int.MinValue;
            for (int i = 0; i < split.Length - 1; i++)
            {
                string chunk = split[i];
                if (chunk != "or")
                {
                    targetValue = int.MinValue;
                }
                //else if (targetValue != int.MinValue) { Console.WriteLine("reinitialization of targetvalue avoided. Target value is now: " + targetValue); }
                if (chunk.Contains(target))
                {
                    targetValue = getValue(chunk, target);
                    //Console.WriteLine("Target value is now: " + targetValue);
                }
            }




            return value;
        }
        static int getValue(string chunk, string target)
        {
            int value = int.MinValue;
            char[] charredChunk = chunk.ToCharArray();
            string valueBuilder = "";
            int j = target.Length;
            while (char.IsDigit(charredChunk[j]) && charredChunk[j] != ' ')
            {
                valueBuilder += charredChunk[target.Length];
                j++;
            }
            value = int.Parse(valueBuilder);

            return value;
        }
        static Dictionary <string, int> askExoValues(List<string> exoNames)
        {
            Dictionary<string, int> exo= new Dictionary<string, int>();
            Console.WriteLine("Now we need to know the values of the exogenous variables.");
            for(int i=0; i < exoNames.Count; i++)
            {
                Console.WriteLine("What is the value of " + exoNames[i] +"?");
                string xValue = Console.ReadLine();
                int number = 0;
                if (int.TryParse(xValue, out number))
                {
                    int value = int.Parse(xValue);
                    exo.Add(exoNames[i], value);
                }
                else
                {
                    Console.WriteLine("Entered value was not an integer!");
                    i--;
                }
            }
            return exo;
        }
        static void printPrerequisites(List<string> exoNames, List<string> endoNames, List<thisVar> vars)
        {
            Console.WriteLine();
            Console.Write("U= {");
            printList(exoNames);
            Console.Write("}, ");
            for (int i = 0; i < vars.Count; i++)
            {
                if (exoNames.Contains(vars[i].name))
                {
                    Console.Write("R(" + vars[i].name + ")= {");
                    for(int j = 0; j < vars[i].range.Count-1; j++)
                    {
                        Console.Write(vars[i].range[j]+", ");
                    }
                    Console.Write(vars[i].range[vars[i].range.Count - 1]+"}, ");
                }
            }
            Console.WriteLine();
            Console.Write("V= {");
            printList(endoNames);
            Console.Write("}, ");
            for (int i = 0; i < vars.Count; i++)
            {
                if (endoNames.Contains(vars[i].name))
                {
                    Console.Write("R(" + vars[i].name + ")= {");
                    for (int j = 0; j < vars[i].range.Count - 1; j++)
                    {
                        Console.Write(vars[i].range[j] + ", ");
                    }
                    Console.Write(vars[i].range[vars[i].range.Count - 1] + "}, ");
                }
            }
            Console.WriteLine();
            Console.WriteLine();
        }
        static void printList(List<string> thisList)
        {
            if (thisList.Count == 0) Console.WriteLine("Tried to print an empty list.");
            for (int i = 0; i < thisList.Count - 1; i++)
            {
                Console.Write(thisList[i] + ", ");
            }
            Console.Write(thisList[thisList.Count - 1]);
        }
        static List<thisVar> getVarRanges(List<thisVar>vars, List<string>varNames, string mainString)
        {
            List<thisVar> varsWithRange = vars;
            string stringBuilder = "";
            string intBuilder = "";
            string lastVarName = "";
            bool wasBuildingName = false;
            char[] chars = mainString.ToCharArray();
            foreach (char c in chars)               // we loop through all the chars
            {
                if (!char.IsDigit(c) && intBuilder != "")  // if the char is not a digit but the intBuilder is not empty, then we have to save the number that was being contructed
                {
                    for (int i = 0; i < varNames.Count; i++)    // 
                    {
                        if (lastVarName== varNames[i] && !varsWithRange[i].range.Contains(int.Parse(intBuilder)))
                        {
                            varsWithRange[i].range.Add(int.Parse(intBuilder));
                            //Console.Write(varsWithRange[i].name + " range is " + intBuilder);
                            //Console.WriteLine();
                        }
                    }
                }
                if (char.IsLetter(c) && char.IsUpper(c))
                {
                    stringBuilder += c;
                    wasBuildingName = true;
                }
                else if (wasBuildingName)
                {
                    lastVarName = stringBuilder;    
                    stringBuilder = "";
                    wasBuildingName = false;
                }
                if (char.IsDigit(c))
                {
                    intBuilder += c;
                }
                else intBuilder = "";
            }

            return varsWithRange;
        }
        static List<thisVar> getVarDomains(List<thisVar>vars, List<string> varNames, string mainString)       //careful, this also gets vars that should keep an empty domain!
        {
            List<thisVar>varsWithDomain = vars;
            char[] chars = mainString.ToCharArray(); 
            string sb = "";                                 // stringbuilder
            string lastName = "";
            string mainName = "";
            bool buildingArrow = false;                     // used to check if := is being made
            bool arrowComplete = false;
            bool buildingName = false;
            bool buildingDouble = false;

            /*
            ST:= 1 if UST = 1; ST:= 0 if UST = 0; ; BT:= 1 if UBT = 1; BT:= 0 if UBT = 0; ;
            */

            for (int i = 0; i < chars.Length; i++)            // running through all of the chars of the mainstring to get all the vars and their domains.
            {
                char c = chars[i];
                //Console.WriteLine("char is " + c);
                if (arrowComplete)
                {
                    if (lastName == "") Console.WriteLine("lastname was empty but shouldn't");
                    mainName = lastName;
                    //Console.WriteLine("mainName is " + mainName);
                    arrowComplete = false;
                    //Console.WriteLine("arrowcomplete set to false in AC");
                }
                if (char.IsLetter(c) && char.IsUpper(c))
                {
                    sb += c;
                    //Console.WriteLine("sb is "+sb);
                    buildingName = true;
                }
                else
                {
                    if (buildingName)  // were saving a name
                    {
                        lastName = sb;
                        //Console.WriteLine("lastname is " + lastName);
                        if (mainName != "")     // if a mainname has been initialized and we're saving a name
                        {
                            //add currentvar to domain of var corresponding to mainvar
                            for (int j = 0; j < varsWithDomain.Count; j++)
                            {
                                if (mainName == varsWithDomain[j].name && !varsWithDomain[j].domain.Contains(lastName)&&mainName!=lastName)     // find the var with this name
                                {
                                    varsWithDomain[j].domain.Add(lastName);     // add the name to the domain of this var
                                    //Console.WriteLine(lastName + " is added to the domain");
                                }
                            }
                        }
                    }
                    sb = "";
                    buildingName = false;
                    
                    if (c == '=' && buildingArrow)
                    {
                        arrowComplete = true;
                        //Console.WriteLine("arrowcomplete is set to true");
                    }
                    else
                    {
                        arrowComplete = false;
                        //Console.WriteLine("arrowcomplete set to false in else");
                    }
                    if (c == ':')
                    {
                        buildingArrow = true;
                        //Console.WriteLine("BA is set true");
                    }
                    else if (c != ':' || c != '=')
                    {
                        buildingArrow = false;
                        //Console.WriteLine("BA set to false by else");
                    }
                    if (c == ';' && buildingDouble)
                    {
                        mainName = "";
                    }
                    if (c == ';')
                    {
                        buildingDouble = true;
                    }
                    else buildingDouble = false;
                }
            }

            return varsWithDomain;
        }
       
        static List<string> getAllVarNames (string mainString)
        {
            string[] chunks = mainString.Split(";;");      // chunk = endogenous variable assignment string = everything until next ;;
            List<string> varNames = new List<string>();
            for (int i = 0; i < chunks.Length - 1; i++)
            {
                varNames.AddRange(getChunkVarNames(chunks[i]));
            }
            varNames = varNames.Distinct().ToList();
            return varNames;
        }
        static string prepareInput()
        {
            string inputString = Console.ReadLine();
            string nocommentInputstring = inputString.Replace("//", "");
            string nosbInputString = nocommentInputstring.Replace(" ", "");
            string strippedInputString = nosbInputString.Trim();
            return strippedInputString;
        }
        static List<string> getChunkVarNames(string chunk)          // gets all the variables from a chunk
        {
            List<string>chunkVarNames = new List<string>();
            int workingOn = 0;                      // keeps track of what kind of information is being processed
            int workingOnWas = 0;                   // keeps track of what kind of information was being processed
            string stringBuilder = "";              // used to reconstruct strings from chars

            char[] charChunk = chunk.ToCharArray();
            for (int i = 0; i < charChunk.Length; i++)
            {
                char currentChar = charChunk[i];
                if (char.IsLetter(currentChar) && char.IsUpper(currentChar)) workingOn = 1;
                else workingOn = 0;

                if (workingOn != workingOnWas)  //if we encounter a new type of char, we might have to save the last bit of info.
                {
                    if (workingOnWas == 1)
                    {
                        chunkVarNames.Add(stringBuilder);
                    }
                    stringBuilder = "";     // var name saved, clearing the stringbuilder so it can save the next var
                }
                stringBuilder += currentChar;
                workingOnWas = workingOn;
            }
            return chunkVarNames;
        }
        static List<string> getEndoVarNames(List<string>allNames, string mainString)
        {
            List<string> endoVarNames = new List<string>();
            //Console.WriteLine("mainstring "+ mainString);
            char[] charred = mainString.ToCharArray();
            string stringBuilder = "";
            bool semicolonOccured = false;            // checks if := occurs so we know there was an endovar before
            for (int i = 0; i < charred.Length; i++)
            {
                if (charred[i] == ':')
                {
                    semicolonOccured = true;
                    continue;                       // skip the rest of this iteration to prevent semicolonOccured from being reset to false
                }

                if (charred[i] == '=' && semicolonOccured && !endoVarNames.Contains(stringBuilder))
                {
                    endoVarNames.Add(stringBuilder);
                }
                if (char.IsLetter(charred[i]) && char.IsUpper(charred[i])) stringBuilder += charred[i];
                else if (!semicolonOccured) stringBuilder = "";
                semicolonOccured = false;
            }
            return endoVarNames;
        }
        static List<string> getExoVarNames(List<string>allNames, List<string> endoNames, string mainString)
        {
            List<string> exoVarNames= new List<string>();
            foreach (string name in allNames)
            {
                if (!endoNames.Contains(name))
                {
                    exoVarNames.Add(name);
                }
            }
            return exoVarNames;
        }
    }
}
