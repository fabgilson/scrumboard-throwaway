using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Utils
{
    public static class GitlabCommitUtils
    {   
        // A list of the special case non-worklog tags that will be handled differently.
        public static List<string> specialTags = new() { "#task", "#time", "#pair" };

        // This regex applies to the second part of a #task tag (e.g. 'T20') and matches successfully when 
        // the letter T (upper or lower) is followed by a number.
        public static string taskTagRegex = @"^\s*(T|t)\d+\s*$";

        // This regex applies to a single section of the second part of a #time tag (e.g. '1h' from the full '1h 10m 9s') 
        // and matches successfully when the section has any number followed by a 'h' or 'm' or 's'.
        public static string timeTagRegex = @"(^\s*\d+(h|m|s)\s*$)";

        // This is a basic regex that applies to the second part of a #pair tag (e.g. abc123,acb234) and matches successfully when 
        // there are two strings comma separated that end with numbers.
        public static string pairTagRegex = @"^\s*\S+\d+,\S+\d+\s*$";

        /// <summary>
        /// Extracts the #task, #time and #pair tag data as well as any normal worklog tags from the given commit message.
        /// Also returns a cleaned description after tags have been removed. Any tags that are malformed will be kept in the description.
        /// -------------------
        /// The idea is to split the commit message on spaces, loop over each section looking for tags, and if a valid tag is found,
        /// store its position for later removal when cleaning the message for the description. 
        /// </summary>
        /// <param name="commitMessage">A commit message from a Gitlab commit</param>
        /// <returns>A thruple containing the valid CommitTags, a list of worklogtags and the cleaned description.</returns>
        public static (CommitTags, List<WorklogTag>, string) ParseCommitMessage(string commitMessage, List<WorklogTag> validWorklogTags) {  
            CommitTags commitTags = new();   
            List<WorklogTag> worklogTags = new();      
            List<string> splitMessage = SplitCommitMessage(commitMessage, true);
            List<int> tagSectionsToRemove = new();          

            for (int i = 0; i < splitMessage.Count; i++) {

                if (splitMessage[i].Contains("#task") && (i+1) < splitMessage.Count) {

                    // Expects '#task T22', where 22 is the InProjectId (not globally unqiue) of a task.
                    var value = Regex.Match(splitMessage[i+1], taskTagRegex).Value;
                    if (value != null && value.Length > 0) value = value.Substring(1);                  
                    if (!long.TryParse(value, out var Id)) continue;
                    commitTags.TaskTag = Id;    
                    tagSectionsToRemove.AddRange(new int[] { i, i+1 });                

                } else if (splitMessage[i].Contains("#pair") && (i+1) < splitMessage.Count ) { 

                    // Grabs the second part of the #pair tag. I.e. The comma separated list of user Ids in '#pair abc123,abc234'.
                    string pairUsers = splitMessage[i+1];
                    if (Regex.Match(pairUsers, pairTagRegex).Success) {
                        commitTags.PairTag = pairUsers;
                        tagSectionsToRemove.AddRange(new int[] { i, i+1 }); 
                    }           

                } else if (splitMessage[i].Contains("#time")) {                   
                    
                    // Processes the second pair of the #time tag string. E.g. #time 1h 3m 10s. And if valid sets the tag duration Timespan.
                    var (duration, timeSegmentCount) = FormatTime(i, splitMessage);                   
                    if (duration != null) {
                        commitTags.TimeTag = duration.Value;                        
                        for (int j = 0; j <= timeSegmentCount; j++) {                         
                            tagSectionsToRemove.Add(i+j);
                        }
                    }                                    

                } else if (splitMessage[i].Length > 1 && validWorklogTags.Any(tag => tag.Name.ToLower() == $"{splitMessage[i].Substring(1)}")) {
                    
                    // Processes the remaining normal worklog tags. E.g. #fix, #document, #chore. Also removes any duplicates.
                    WorklogTag tag = validWorklogTags.Where(tag => tag.Name.ToLower() == $"{splitMessage[i].Substring(1)}").FirstOrDefault();
                    if (tag != null && !worklogTags.Any(t => t.Id == tag.Id)) worklogTags.Add(tag);
                    tagSectionsToRemove.Add(i);

                }
            }

            string cleanDescription = ProcessDescription(commitMessage, tagSectionsToRemove); 
           
            return (commitTags, worklogTags, cleanDescription);
        }

        /// <summary>
        /// Processes the commit message string by removing all valid tags.
        /// Newlines (that aren't at the start or end) are also kept in the cleaned message.
        /// </summary>
        /// <param name="commitMessage">The commit message to clean</param>
        /// <param name="tagSectionsToRemove">A list of int indexes that contains all locations of tags (and their parameters)</param>
        /// <returns>A cleaned description.</returns>
        public static string ProcessDescription(string commitMessage, List<int> tagSectionsToRemove)
        {
            List<string> splitMessage = SplitCommitMessage(commitMessage, false);
            tagSectionsToRemove.Sort();
            // Must remove in reverse order while iterating
            tagSectionsToRemove.Reverse();
            foreach (int section in tagSectionsToRemove) {
                splitMessage.RemoveAt(section);
            }
            
            string cleanDescription = string.Join(" ", splitMessage); 
            // Handle cases where newlines are in strange places in the message
            cleanDescription = cleanDescription.Replace(" \n ", "\n"); 
            cleanDescription = cleanDescription.Replace("\n  ", "\n");  
            cleanDescription = cleanDescription.Replace("\n ", "\n"); 
            cleanDescription = cleanDescription.Replace(" \n", "\n");           
    
            return cleanDescription.Trim();
        }

        /// <summary>
        /// Gets the count of time segments in the given splitMessage. I.e. how long is the second part of the #time tag.
        /// Examples:
        /// #time 1h 5m 30s has three segments
        /// #time 5m 30s has two segments
        /// #time 30s has one
        /// </summary>
        /// <param name="timeTagIndex">The index of the time tag in the splitMessageLower list.</param>
        /// <param name="splitMessageLower">A list of strings which are the space/newline separated contents of the commit message</param>
        /// <returns>The number of time segments</returns>
        public static int GetTimeSegmentCount(int timeTagIndex, List<string> splitMessageLower)
        {
            int count = 0;
            for (int j = 1; j < 4; j++) {                               
                if ((timeTagIndex+j) < splitMessageLower.Count && 
                        Regex.Match(splitMessageLower[timeTagIndex+j].ToLower(), timeTagRegex).Success) 
                {
                    count += 1;
                } else {
                    break;
                }
            }            
            return count;
        }

        /// <summary>
        /// Formats the second part of the #time tag and then uses DurationUtils to transform the string into a timespan.
        /// </summary>
        /// <param name="splitMessageIndex">The current i value for the outer for loop over the splitMessage.</param>
        /// <param name="splitMessageLower">A list of strings which are the space/newline separated contents of the commit message</param>
        /// <returns>A timespan from the given commit message #time tag</returns>
        public static (TimeSpan?, int) FormatTime(int splitMessageIndex, List<string> splitMessageLower) 
        {
            string time = "";
            int timeSegmentCount = GetTimeSegmentCount(splitMessageIndex, splitMessageLower);
            for (int i = 1; i < timeSegmentCount+1; i++) {                
                time += $" {splitMessageLower[splitMessageIndex+i].Trim()}";                
            }   

            TimeSpan? duration = DurationUtils.TimeSpanFromDurationString(time);        
            if (duration.HasValue && duration.Value <= TimeSpan.Zero) duration = null;              
            return (duration, timeSegmentCount);
        }

        /// <summary>
        /// Splits the given commit message string on spaces and adds spaces around each newline so they can be kept in the worklog description.
        /// All other newline types are also converted to unix newlines.
        /// Can optionally convert the string to lowercase before splitting.
        /// </summary>
        /// <param name="message">A commit message string</param>
        /// <param name="lowerCase">A boolean containing whether to convert the string to lowercase or not.</param>
        /// <returns>A list of the split sections of the given string (optionally lowercase)</returns>
        public static List<string> SplitCommitMessage(string message, bool lowerCase)  
        {
            string finalString = message;
            if (lowerCase) finalString = finalString.ToLower();  
            
            finalString = finalString.Replace("\r\n", "\n");
            finalString = finalString.Replace("\r", "\n");  
            finalString = finalString.Replace("\n", " \n ");    
            return finalString.Split(" ").ToList();                       
        } 

    }
}