use byteorder::{LittleEndian, WriteBytesExt};
use fst::SetBuilder;
use std::collections::BTreeSet;
use std::fs::File;
use std::io::{BufRead, BufReader, Write};
use std::path::{Path, PathBuf};

const MAGIC: &[u8; 8] = b"YANGIDAT";
const VERSION: u32 = 1;
const HEADER_SIZE: u64 = 64;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("=== YangiAlifbo Binary Data File Generator ===");

    let repo_root = find_repo_root();
    println!("Repo root: {}", repo_root.display());

    let roots_path = repo_root.join("data").join("roots.txt");
    let exceptions_path = repo_root.join("data").join("exceptions.txt");
    let reject_path = repo_root
        .join("src")
        .join("Core")
        .join("Dictionaries")
        .join("english_reject.txt");
    let output_bin_path = repo_root.join("data").join("yangi_data.bin");

    println!("Loading roots from {}...", roots_path.display());
    let roots = load_sorted_set(&roots_path)?;
    println!("Loaded {} unique sorted roots.", roots.len());

    println!("Loading exceptions from {}...", exceptions_path.display());
    let exceptions = load_sorted_set(&exceptions_path)?;
    println!("Loaded {} unique sorted exceptions.", exceptions.len());

    println!("Loading reject words from {}...", reject_path.display());
    let rejects = load_sorted_set(&reject_path)?;
    println!("Loaded {} unique sorted reject words.", rejects.len());

    // 1. Build Roots FST
    println!("Building Roots FST...");
    let roots_bytes = build_fst_set(&roots)?;
    println!("Roots FST size: {} bytes ({:.2} KB)", roots_bytes.len(), roots_bytes.len() as f64 / 1024.0);

    // 2. Build Exceptions FST
    println!("Building Exceptions FST...");
    let exceptions_bytes = build_fst_set(&exceptions)?;
    println!("Exceptions FST size: {} bytes ({:.2} KB)", exceptions_bytes.len(), exceptions_bytes.len() as f64 / 1024.0);

    // 3. Build Reject List FST
    println!("Building Reject List FST...");
    let rejects_bytes = build_fst_set(&rejects)?;
    println!("Reject List FST size: {} bytes ({:.2} KB)", rejects_bytes.len(), rejects_bytes.len() as f64 / 1024.0);

    // Calculate offsets
    let roots_offset = HEADER_SIZE;
    let roots_len = roots_bytes.len() as u64;

    let exceptions_offset = roots_offset + roots_len;
    let exceptions_len = exceptions_bytes.len() as u64;

    let reject_offset = exceptions_offset + exceptions_len;
    let reject_len = rejects_bytes.len() as u64;

    let total_size = reject_offset + reject_len;

    println!("\n--- Header Layout ---");
    println!("Header Size:       {} bytes", HEADER_SIZE);
    println!("Roots:             offset {}..{}, len {} bytes", roots_offset, roots_offset + roots_len, roots_len);
    println!("Exceptions:        offset {}..{}, len {} bytes", exceptions_offset, exceptions_offset + exceptions_len, exceptions_len);
    println!("Reject List:       offset {}..{}, len {} bytes", reject_offset, reject_offset + reject_len, reject_len);
    println!("Total Binary Size: {} bytes ({:.2} KB / {:.2} MB)", total_size, total_size as f64 / 1024.0, total_size as f64 / (1024.0 * 1024.0));

    // Write binary file
    let mut out_file = File::create(&output_bin_path)?;

    // 64-byte header
    out_file.write_all(MAGIC)?;
    out_file.write_u32::<LittleEndian>(VERSION)?;
    out_file.write_u32::<LittleEndian>(0)?; // flags
    out_file.write_u64::<LittleEndian>(roots_offset)?;
    out_file.write_u64::<LittleEndian>(roots_len)?;
    out_file.write_u64::<LittleEndian>(exceptions_offset)?;
    out_file.write_u64::<LittleEndian>(exceptions_len)?;
    out_file.write_u64::<LittleEndian>(reject_offset)?;
    out_file.write_u64::<LittleEndian>(reject_len)?;

    // Write sections
    out_file.write_all(&roots_bytes)?;
    out_file.write_all(&exceptions_bytes)?;
    out_file.write_all(&rejects_bytes)?;

    out_file.flush()?;

    println!("\nSuccessfully generated: {}", output_bin_path.display());
    Ok(())
}

fn load_sorted_set(path: &Path) -> Result<BTreeSet<String>, Box<dyn std::error::Error>> {
    let file = File::open(path)?;
    let reader = BufReader::new(file);
    let mut set = BTreeSet::new();

    for line in reader.lines() {
        let line = line?;
        let trimmed = line.trim().to_lowercase();
        if !trimmed.is_empty() {
            set.insert(trimmed);
        }
    }
    Ok(set)
}

fn build_fst_set(entries: &BTreeSet<String>) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
    let mut buffer = Vec::new();
    let mut builder = SetBuilder::new(&mut buffer)?;

    for entry in entries {
        builder.insert(entry.as_bytes())?;
    }
    builder.finish()?;
    Ok(buffer)
}

fn find_repo_root() -> PathBuf {
    let mut dir = std::env::current_dir().unwrap_or_default();
    loop {
        if dir.join("YangiAlifbo.sln").exists() || dir.join(".agent").exists() {
            return dir;
        }
        if !dir.pop() {
            break;
        }
    }
    std::env::current_dir().unwrap_or_default()
}
