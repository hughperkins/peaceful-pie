using System;

[Serializable]
class PeacefulPieException : Exception {
    string? message;
    public PeacefulPieException(string? message = null) {
        this.message = message;
    }
}
