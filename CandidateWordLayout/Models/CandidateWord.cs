namespace CandidateWordLayout.Models;

public class CandidateWord
{
    public int Index { get; set; }
    public int Number => Index + 1;
    public string Word { get; set; } = "";
}