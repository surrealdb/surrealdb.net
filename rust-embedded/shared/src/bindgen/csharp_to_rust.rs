/// # Safety
///
/// This function converts a C# byte array into a Vec<u8>.
pub unsafe fn convert_csharp_to_rust_bytes(bytes: *const u8, len: i32) -> Vec<u8> {
    let slice = std::slice::from_raw_parts(bytes, len as usize);
    slice.to_vec()
}

/// # Safety
///
/// This function converts an (UTF-16) C# string (u16 array) into a Rust String.
pub unsafe fn convert_csharp_to_rust_string_utf16(bytes: *const u16, len: i32) -> String {
    let slice = std::slice::from_raw_parts(bytes, len as usize);
    String::from_utf16(slice).unwrap()
}
