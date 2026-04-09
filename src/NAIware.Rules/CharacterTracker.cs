namespace NAIware.Rules;

/// <summary>
/// A character tracker used in expression validation.
/// </summary>
internal class CharacterTracker
{
    private readonly char _characterTracked;
    private readonly int _tokenPosition;
    private readonly bool _isCharValid;

    private CharacterTracker()
    {
        _characterTracked = ' ';
        _tokenPosition = -1;
    }

    public CharacterTracker(char formulaCharacter, int tokenPosition)
    {
        _characterTracked = formulaCharacter;
        _tokenPosition = tokenPosition;
        _isCharValid = char.IsDigit(formulaCharacter) ||
                       char.IsLetter(formulaCharacter) ||
                       formulaCharacter == ' ';

        if (!_isCharValid)
        {
            _isCharValid = _characterTracked switch
            {
                '"' or '*' or '/' or '-' or '+' or '(' or ')' or '#' or
                '=' or '$' or ',' or '.' or '>' or '<' => true,
                _ => false
            };
        }
    }

    public bool IsValid => _isCharValid;
    public char Character => _characterTracked;
    public int Position => _tokenPosition;
}
