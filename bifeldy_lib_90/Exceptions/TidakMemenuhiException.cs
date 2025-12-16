namespace bifeldy_lib_90.Exceptions {

    public sealed class TidakMemenuhiException : Exception {

        public TidakMemenuhiException() { }

        public TidakMemenuhiException(string message) : base(message) { }

        public TidakMemenuhiException(string message, Exception inner) : base(message, inner) { }

    }

}