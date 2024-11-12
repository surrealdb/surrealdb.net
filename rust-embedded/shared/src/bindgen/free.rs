use super::byte_buffer::ByteBuffer;

/// # Safety
///
/// This function is used to free Rust memory from a C# binding.
#[no_mangle]
pub unsafe extern "C" fn free_u8_buffer(buffer: *mut ByteBuffer) {
    let buf = Box::from_raw(buffer);
    // drop inner buffer, if you need Vec<u8>, use buf.destroy_into_vec() instead.
    buf.destroy();
}
