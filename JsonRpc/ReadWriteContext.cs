namespace JsonRpc {
    public class ReadContext : IDisposable {
        public ReadContext(ReaderWriterLock a_lock, int a_ms = 0) {
            m_lock = a_lock;
            m_lock.AcquireReaderLock(a_ms);
        }

        public void Dispose() {
            m_lock.ReleaseReaderLock();
        }

        ReaderWriterLock m_lock;
    }

    public class WriteContext : IDisposable {
        public WriteContext(ReaderWriterLock a_lock, int a_ms = 0) {
            m_lock = a_lock;
            m_lock.AcquireWriterLock(a_ms);
        }

        public void Dispose() {
            m_lock.ReleaseWriterLock();
        }

        ReaderWriterLock m_lock;
    }
}
