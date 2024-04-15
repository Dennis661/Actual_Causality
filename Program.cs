using System.Collections.Generic;
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
                Z:=1 if X=1,Y=1;or; X=1,Y=0;or; X=0,Y=1; Z:=0 if X=0,Y=0; ;
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
            Console.WriteLine("Welcome. This program verifies actual causality by using one of two methods. Please provide the program with an input string.");
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

            printList(allVarNames);
            Printprerequisites(exoVarNames, endoVarNames, vars);
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
                            Console.Write(varsWithRange[i].name + " range is " + intBuilder);
                            Console.WriteLine();
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
            string[] chunk = mainString.Split(";;");
            string sb = "";
            for(int i = 0; i<chunk.Length-1;i++)            //every chunk starts with an endovar and the other vars are its domain
            {
                char[] charred = chunk[i].ToCharArray();
                int index = 0;
                while (char.IsDigit(charred[index]) && char.IsUpper(charred[index]))        //get mainvar
                {
                    sb += charred[index];
                    index++;
                }
                for(int k = 0; k<varsWithDomain.Count;k++)  //get domain should get done
                {
                    if (varsWithDomain[k].name == sb)
                    {

                    }
                }
                for(int j = index; j < charred.Length; j++)
                {
                    if (char.IsLetter(charred[i]) && char.IsUpper(charred[i])) sb += charred[i];
                    else if (!char.IsDigit(charred[i]) && !char.IsUpper(charred[i])) ;
                }
                
            }

            return varsWithDomain;
        }
        static void Printprerequisites(List<string>exoNames, List<string>endoNames, List<thisVar>vars)
        {
            Console.Write("U= ");
            printList(exoNames);
            Console.Write("V= ");
            printList(endoNames);
        }
        static void printList(List<string>thisList)
        {
            if (thisList.Count == 0) Console.WriteLine("Tried to print an empty list. What a noob!");
            for(int i=0; i<thisList.Count-1; i++)
            {
                Console.Write(thisList[i]+", ");
            }
            Console.Write(thisList[thisList.Count - 1]+".");
            Console.Write("\n");
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
            string nosbInputString = inputString.Replace(" ", "");
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
            Console.WriteLine("mainstring "+ mainString);
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
