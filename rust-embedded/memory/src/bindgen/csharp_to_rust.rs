pub fn convert_csharp_to_rust_bytes(bytes: *const u8, len: i32) -> Vec<u8> {
    unsafe {
        let slice = std::slice::from_raw_parts(bytes, len as usize);
        slice.to_vec()
    }
}
