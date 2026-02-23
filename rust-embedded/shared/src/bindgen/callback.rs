use surrealdb::rpc::format::cbor::encode;
use surrealdb_types::Value;

use super::{alloc::alloc_u8_buffer, byte_buffer::ByteBuffer};

type GCHandlePtr = isize;

#[repr(C)]
pub struct RustGCHandle {
    ptr: GCHandlePtr,
    drop_callback: extern "C" fn(GCHandlePtr),
}

impl Drop for RustGCHandle {
    fn drop(&mut self) {
        (self.drop_callback)(self.ptr);
    }
}

#[repr(C)]
pub struct SuccessAction {
    handle: RustGCHandle,
    callback: unsafe extern "C" fn(GCHandlePtr, *mut ByteBuffer),
}

impl SuccessAction {
    /// # Safety
    ///
    /// Invokes the expected Success action.
    pub unsafe fn invoke(&self, value: *mut ByteBuffer) {
        unsafe {
            (self.callback)(self.handle.ptr, value);
        }
    }
}

#[repr(C)]
pub struct FailureAction {
    handle: RustGCHandle,
    callback: unsafe extern "C" fn(GCHandlePtr, *mut ByteBuffer),
}

impl FailureAction {
    /// # Safety
    ///
    /// Invokes the expected Failure action.
    pub unsafe fn invoke(&self, value: *mut ByteBuffer) {
        unsafe {
            (self.callback)(self.handle.ptr, value);
        }
    }
}

fn value_to_buffer(value: Value) -> Result<*mut ByteBuffer, ()> {
    let output = encode(value).map_err(|_| ())?;
    Ok(alloc_u8_buffer(output))
}

pub fn send_success(bytes: Vec<u8>, success: SuccessAction) {
    let buffer = alloc_u8_buffer(bytes);
    unsafe { success.invoke(buffer) };
}

pub fn send_failure(error: &str, action: FailureAction) {
    let value = Value::String(error.into());

    match value_to_buffer(value) {
        Ok(buffer) => unsafe { action.invoke(buffer) },
        Err(_) => panic!("Failed to serialize Value"),
    }
}
