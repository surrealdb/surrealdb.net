use super::byte_buffer::ByteBuffer;

pub fn alloc_u8_buffer(vec: Vec<u8>) -> *mut ByteBuffer {
    let buffer = ByteBuffer::from_vec(vec);
    Box::into_raw(Box::new(buffer))
}
