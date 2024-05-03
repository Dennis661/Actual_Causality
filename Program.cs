namespace Actual_Causality
{
    internal class Program
    {
        /*  examples of inputstrings:

                1. Yes-Gate
                2. Example with bigger domains
                3. Or-Gate
                4. Example of multiple digit variables
                5. Suzy-Billy rock throwing example

                B:= 1 if A=1; B:=0 if A=0;;
                B:= 1 if A=1; B:=0 if A=0; or; A=2; A=3;A=28;;
                Z:=1 if X=1,Y=1;or; X=1,Y=0;or; X=0,Y=1; Z:=0 if X=0,Y=0;;
                Z:=11ifX=1,Y=1;or;X=1,Y=0;;OZD:=9999or;X=0,Y=1;Z:=0ifX=0,Y=0;;
                ST:= 1 if UST = 1; ST:= 0 if UST=0;;BT:= 1 if UBT = 1; BT:= 0 if UBT = 0;; SH:= 1 if ST = 1; SH:= 0 if ST = 0;;BH:= 1 if BT = 1,SH = 0; BH:= 0 if BT = 0,SH = 0; or; BT = 0,SH = 1; or; BT = 1,SH = 1;;BS:= 1 if SH = 1,BH = 1; or; SH = 1,BH = 0; or; SH = 0,BH = 1;  BS:= 0 if SH = 0,BH = 0; ;


                // prisoner input string
                D:=1ifA=1,B=1,C=1;or;A=1,B=1,C=0;or;A=0,B=0,C=1;or;A=0,B=1,C=1;or;A=1,B=0,C=1;;
         */
        struct thisVar
        {
            public string name;
            public List<int> range;
            public List<string> domain;
        }

        struct CalculatedValues
        {
            public Dictionary<string, int> known;
            public Dictionary<string, int> foundAtIter;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome. This program is used for research on a good definition of Actual Causality. \nPlease provide the program with an input string.");
            string modelString = prepareInput();
            List<string> allVarNames = getAllVarNames(modelString);
            List<string> endoVarNames = getEndoVarNames(allVarNames, modelString);
            List<string> exoVarNames = getExoVarNames(allVarNames, endoVarNames, modelString);

            List<thisVar> vars = new List<thisVar>();
            foreach (string v in allVarNames)               // creating the variables and adding their names
            {
                thisVar currentVar = new thisVar();
                currentVar.name = v;
                currentVar.range = new List<int>();
                currentVar.domain = new List<string>();
                vars.Add(currentVar);
            }
            vars = getVarRanges(vars, allVarNames, modelString);     // adding ranges to the variables
            vars = getVarDomains(vars, allVarNames, modelString);    // adding domains to the variables

            printPrerequisites(exoVarNames, endoVarNames, vars);
            Dictionary<string, int> exoValues = askExoValues(exoVarNames);

            KeyValuePair<string, int> defaultIntervention = new KeyValuePair<string, int>();        // empty intervention is passed to calculate the original values

            //taking copies so the call wont change their values
            Dictionary<string, int> copyEXV = new Dictionary<string, int>(exoValues);
            List<string> copyENN = new List<string>(endoVarNames);
            List<thisVar> copyVars = new List<thisVar>(vars);
            string copyMS = modelString;

            var originalValuesAsStruct = calculateValuesAsStruct(copyEXV, copyENN, copyVars, copyMS, defaultIntervention);
            var originalValues = originalValuesAsStruct.known;

            Console.WriteLine("\nNow which variable would you like to investigate as a possible cause?");
            string possibleCause = Console.ReadLine();
            Console.WriteLine("With which value for " + possibleCause + "?");
            int pbValue = int.Parse(Console.ReadLine());
            Console.WriteLine("On the effect on which other variable?");
            string allegedDependant = Console.ReadLine();
            Console.WriteLine("What corresponding value does " + allegedDependant + " have?");
            int adValue = int.Parse(Console.ReadLine());
            Console.WriteLine("And for which definition would you like to have your answer? Write 1 for cNess, 2 for DC and 3 for HPm.");
            int chosenMethod = int.Parse(Console.ReadLine());
            Console.WriteLine();

            // now we add an intervention, to check if this still holds.
            KeyValuePair<string, int> pc = new KeyValuePair<string, int>(possibleCause, pbValue);
            KeyValuePair<string, int> ad = new KeyValuePair<string, int>(allegedDependant, adValue);    // alleged dependant is commonly referred to as (greek) phi in logic
            Dictionary<string, int> investigation = new Dictionary<string, int>();
            investigation.Add(pc.Key, pbValue);
            investigation.Add(ad.Key, adValue);

            Console.WriteLine("Chosen method is: " + chosenMethod);

            var causal = isCause(originalValuesAsStruct, investigation, vars, exoValues, endoVarNames, modelString,
                chosenMethod);

            if (causal)
            {
                Console.WriteLine(pc + " is a cause of " + ad + " under this context in this model!");
                if(chosenMethod ==1) Console.WriteLine("Because had " + pc.Key + " not been " + pc.Value + ", then " + ad.Key + " would not have been " + ad.Value + ".");
            }
            else
            {
                Console.WriteLine(pc + " is not a cause of " + ad + " under this context in this model!");
                if(chosenMethod==1) Console.WriteLine("Because had " + pc.Key + " not been " + pc.Value + ", then " + ad.Key + " would still have been " + ad.Value + ".");
            }
        }

        static bool isCause(CalculatedValues ogValues, Dictionary<string, int> investigation, List<thisVar> vars, Dictionary<string, int> exoValues, List<string> endoVarNames, string modelString, int chosenMethod = 1)
        {
            var key = investigation.ElementAt(1).Key;
            ogValues.foundAtIter.TryGetValue(key, out int iterOfPhiOG);

            if (originalCheck(ogValues.known, investigation))     // first, we check if the values given were true in the original model
            {                                               // we now go look if any assignment of the possible cause variable might have changed the value of the alleged dependant
                foreach (thisVar v in vars)
                {
                    Console.WriteLine(v.name);
                }
                List<int> possibleCauseRange = getRange(investigation.ElementAt(0).Key, vars);
                possibleCauseRange.Distinct();
                possibleCauseRange.Remove(investigation.ElementAt(0).Value);
                foreach (int pValue in possibleCauseRange)
                {
                    KeyValuePair<string, int> intervention = new KeyValuePair<string, int>(investigation.ElementAt(0).Key, pValue);

                    var intervenedValuesAsStruct = calculateValuesAsStruct(exoValues, endoVarNames, vars, modelString, intervention);
                    var intervenedValues = intervenedValuesAsStruct.known;
                    var intervenedFoundAtIter = intervenedValuesAsStruct.foundAtIter;

                    if (chosenMethod == 2)
                    {
                        var hasValue = intervenedFoundAtIter.TryGetValue(key, out int iterOfPhiAfterIntervention);
                        if (hasValue)
                        {
                            Console.WriteLine("Phi found at a different iteration!");
                            if (iterOfPhiOG != iterOfPhiAfterIntervention) return true;         // if the iteration in which phi is found is different, DC states that the possible cause is indeed also a cause.
                        }
                    }

                    //Console.WriteLine(investigation.ElementAt(1).Value + " vs " + intervenedValues[investigation.ElementAt(1).Key]);
                    if (investigation.ElementAt(1).Value != intervenedValues[investigation.ElementAt(1).Key])   // alleged dependant: compare original value with new value
                    {
                        Console.WriteLine("Computational step changed!");
                        return true;                // if those are not the same, it means the value has changed. And since the only thing we changed was our possible cause, it is an actual cause.
                    }
                }
            }
            else
            {
                Console.WriteLine("Entered values were not the case in the original model!");
                Console.WriteLine("Therefore the we cannot say anything about causality. \n");
            }
            return false;
        }
        static bool originalCheck(Dictionary<string, int> ogValues, Dictionary<string, int> investigation)
        {
            foreach (KeyValuePair<string, int> kvp in ogValues)
            {
                if (kvp.Key == investigation.ElementAt(0).Key)   // matches the key of possibleCause
                {
                    if (kvp.Value != investigation.ElementAt(0).Value)       // the values are not the same
                    {
                        return false;
                    }
                }
                if (kvp.Key == investigation.ElementAt(1).Key)   // matches the key of allegedDependant
                {
                    if (kvp.Value != investigation.ElementAt(1).Value)       // the values are not the same
                    {
                        return false;
                    }
                }
            }
            return true;        // if you don't see any problems, it might be because it is not wrong
        }
        static List<int> getRange(string pCause, List<thisVar> vars)
        {
            List<int> result = new List<int>();
            foreach (thisVar tV in vars)
            {
                if (tV.name == pCause)
                {
                    result = tV.range;
                    break;
                }
            }

            return result;
        }
        static CalculatedValues calculateValuesAsStruct(Dictionary<string, int> known, List<string> unknown,
            List<thisVar> vars, string modelString, KeyValuePair<string, int> intervention)
        {
            Dictionary<string, int>
                tempKnown =
                    new Dictionary<string, int>(); // this one starts off empty, as we use it to save temporary values.
            Dictionary<string, int> foundAtIter = new Dictionary<string, int>();
            int iter = 0;
            if (intervention.Key != null)
            {
                Console.WriteLine("Reached this with an intervention!");
            }

            if (unknown.Count == 0)
            {
                Console.WriteLine("Empty unknown");
            }

            while
                (unknown.Count > 0 &&
                 iter < 10) // we continue until there are no values unknown anymore, therefore we calculated all values
            {
                Console.WriteLine("\n----------This is iteration " + iter + ".----------");
                Console.WriteLine("This is in unknown:");
                printList(unknown);
                Console.Write("\n");

                Console.WriteLine();
                for (int i = 0; i < unknown.Count; i++) // for every variable in unknown
                {
                    string targetVariable = unknown[i];
                    int value = tryGetValue(unknown[i], known, vars,
                        modelString); // we try to calculate the value in this iter
                    if (value != int
                            .MinValue) // if it is not the default value of the tryGetValue function, meaning it got a value
                    {
                        tempKnown.Add(targetVariable, value);
                        unknown.Remove(targetVariable);
                        i--; // This is crucial since the delete() operator already moves every item one up the list
                    }
                }

                foreach (KeyValuePair<string, int> pair in tempKnown) // any intervention will take place here
                {
                    if (pair.Key == intervention.Key)
                    {
                        known.Add(pair.Key, intervention.Value);
                        Console.WriteLine("After intervention, added to known: " + pair.Key + ", " + intervention.Value);
                    }
                    else
                    {
                        known.Add(pair.Key, pair.Value);
                        Console.WriteLine("Added to known: " + pair.Key + ", " + pair.Value);
                    }
                    foundAtIter.Add(pair.Key, iter);
                }

                tempKnown.Clear();
                iter++;
            }
            Console.WriteLine();
            if(intervention.Key==null)
            {
                Console.WriteLine("These values are found for the variables:");
                foreach (KeyValuePair<string, int> pair in known)
                {
                    Console.Write(pair.Key + "=" + pair.Value + ", ");
                }
                Console.WriteLine("\n");
                Console.WriteLine("The variables are found at these iterations: ");
                foreach (KeyValuePair<string, int> kvp in foundAtIter)
                {
                    Console.Write(kvp.Key + " at " + kvp.Value+", ");
                }
                Console.WriteLine();
            }

            var values = new CalculatedValues();
            values.known = known;
            values.foundAtIter = foundAtIter;

            return values;
        }
        // ST:=1ifUST=1;ST:=0ifUST=0   ;;BT:= 1 if UBT = 1; BT:= 0 if UBT = 0;; SH:= 1 if ST = 1; SH:= 0 if ST = 0;;BH:= 1 if BT = 1,SH = 0; BH:= 0 if BT = 0,SH = 0; or; BT = 0,SH = 1; or; BT = 1,SH = 1;;    BS:= 1 if SH = 1,BH = 1; or; SH = 1,BH = 0; or; SH = 0,BH = 1;  BS:= 0 if SH = 0,BH = 0; ;

        static int tryGetValue(string varName, Dictionary<string, int> known, List<thisVar> vars, string modelString)
        {
            int value = int.MinValue;                               // default is the minimal int value, in case we cannot calculate any other value
            List<string> newKnown = new List<string>();              // intersection of domain of var and actual tempknown
            foreach (thisVar v in vars)         // search through all vars
            {
                if (v.name == varName)           // get the one with the same name as our varname
                {
                    foreach (string d in v.domain)   // check for all vars in the domain
                    {
                        foreach (KeyValuePair<string, int> kvp in known) // check for the intersection with the variables in the Known list
                        {
                            if (d == kvp.Key)               // if the names are the same, it means the variable is both known and in the domain
                            {
                                //Console.WriteLine("Intersection between domain of " + v.name + " and known list found, namely: " + d);
                                newKnown.Add(kvp.Key + "=" + kvp.Value);
                            }
                        }
                    }
                }
            }
            //          B:= 1 if A = 1; B:= 0 if A = 0; 

            string target = varName + ":=";
            string mainModified = modelString.Replace(";;", ";");
            string[] split = mainModified.Split(';');
            int targetValue = int.MinValue;
            HashSet<int> values = new HashSet<int>();
            for (int i = 0; i < split.Length - 1; i++)
            {
                string chunk = split[i];
                //Console.WriteLine("Chunk is: " + chunk);
                if (chunk == "or")                  // way to get past the 'or' statements
                {
                    continue;
                }
                if (chunk.Contains(target))         // get target value
                {
                    targetValue = getValue(chunk, target);
                    //Console.WriteLine("Targetvalue currently is: " + targetValue);
                }
                if (newKnown.All(chunk.Contains) && targetValue != int.MinValue)      // check for containment and that the targetvalue is not reset to the default
                {
                    values.Add(targetValue);                // add the most recent encountered target value
                    //Console.WriteLine("Targetvalue added! --> " + targetValue);
                }
            }
            if (values.Count == 1)
            {
                value = values.First();
                //Console.WriteLine("The value of " + varName + " is " + value);
            }
            return value;
        }
        //                 Z:=1 if X=1,Y=1;or; X=1,Y=0;or; X=0,Y=1; Z:=0 if X=0,Y=0;;

        static int getValue(string chunk, string target)
        {
            //Console.WriteLine("Chunk is: " + chunk);
            int value = int.MinValue;
            char[] charredChunk = chunk.ToCharArray();
            string valueBuilder = "";
            int j = target.Length;
            while (char.IsDigit(charredChunk[j]) && charredChunk[j] != ' ')
            {
                valueBuilder += charredChunk[target.Length];
                j++;
            }
            if (valueBuilder.Length == 0) { Console.WriteLine("oh no, empty!"); }
            value = int.Parse(valueBuilder);

            return value;
        }
        static Dictionary<string, int> askExoValues(List<string> exoNames)
        {
            Dictionary<string, int> exo = new Dictionary<string, int>();
            Console.WriteLine("Now we need to know the values of the exogenous variables.");
            for (int i = 0; i < exoNames.Count; i++)
            {
                Console.WriteLine("What is the value of " + exoNames[i] + "?");
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
                    i--;                // give user another try.
                }
            }
            Console.WriteLine();
            Console.WriteLine("Thank you for entering the values of the exogenous variables. Calculating the original values now.");
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
                    for (int j = 0; j < vars[i].range.Count - 1; j++)
                    {
                        Console.Write(vars[i].range[j] + ", ");
                    }
                    Console.Write(vars[i].range[vars[i].range.Count - 1] + "}, ");
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
        static List<thisVar> getVarRanges(List<thisVar> vars, List<string> varNames, string modelString)
        {
            List<thisVar> varsWithRange = vars;
            string stringBuilder = "";
            string intBuilder = "";
            string lastVarName = "";
            bool wasBuildingName = false;
            char[] chars = modelString.ToCharArray();
            foreach (char c in chars)               // we loop through all the chars
            {
                if (!char.IsDigit(c) && intBuilder != "")  // if the char is not a digit but the intBuilder is not empty, then we have to save the number that was being contructed
                {
                    for (int i = 0; i < varNames.Count; i++)
                    {
                        if (lastVarName == varNames[i] && !varsWithRange[i].range.Contains(int.Parse(intBuilder)))
                        {
                            varsWithRange[i].range.Add(int.Parse(intBuilder));
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
        static List<thisVar> getVarDomains(List<thisVar> vars, List<string> varNames, string modelString)       //careful, this also gets vars that should keep an empty domain!
        {
            List<thisVar> varsWithDomain = vars;
            char[] chars = modelString.ToCharArray();
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

            for (int i = 0; i < chars.Length; i++)            // running through all of the chars of the modelString to get all the vars and their domains.
            {
                char c = chars[i];
                if (arrowComplete)
                {
                    if (lastName == "") Console.WriteLine("lastName is empty but shouldn't");
                    mainName = lastName;
                    arrowComplete = false;
                }
                if (char.IsLetter(c) && char.IsUpper(c))
                {
                    sb += c;
                    buildingName = true;
                }
                else
                {
                    if (buildingName)  // were saving a name
                    {
                        lastName = sb;
                        if (mainName != "")     // if a mainname has been initialized and we're saving a name
                        {
                            //add currentvar to domain of var corresponding to mainvar
                            for (int j = 0; j < varsWithDomain.Count; j++)
                            {
                                if (mainName == varsWithDomain[j].name && !varsWithDomain[j].domain.Contains(lastName) && mainName != lastName)     // find the var with this name
                                {
                                    varsWithDomain[j].domain.Add(lastName);     // add the name to the domain of this var
                                }
                            }
                        }
                    }
                    sb = "";
                    buildingName = false;

                    if (c == '=' && buildingArrow)
                    {
                        arrowComplete = true;
                    }
                    else
                    {
                        arrowComplete = false;
                    }
                    if (c == ':')
                    {
                        buildingArrow = true;
                    }
                    else if (c != ':' || c != '=')
                    {
                        buildingArrow = false;
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

        static List<string> getAllVarNames(string modelString)
        {
            string[] chunks = modelString.Split(";;");      // chunk = endogenous variable assignment string = everything until next ;;
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
        static List<string> getChunkVarNames(string chunk)          // gets all the variable names from a chunk
        {
            List<string> chunkVarNames = new List<string>();
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
        static List<string> getEndoVarNames(List<string> allNames, string modelString)
        {
            List<string> endoVarNames = new List<string>();
            char[] charred = modelString.ToCharArray();
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
        static List<string> getExoVarNames(List<string> allNames, List<string> endoNames, string modelString)
        {
            List<string> exoVarNames = new List<string>();
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
