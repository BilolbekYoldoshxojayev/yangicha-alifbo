declare namespace wasm_bindgen {
    /* tslint:disable */
    /* eslint-disable */

    export function chatNormalize(word: string): string;

    export function convertText(input: string, mode?: string | null, slang?: boolean | null, use_ml?: boolean | null): string;

    export function convertWord(word: string, mode?: string | null, slang?: boolean | null, use_ml?: boolean | null): string;

    export function isForeignOrReject(word: string): boolean;

    export function isUzbek(word: string, mode?: string | null, use_ml?: boolean | null): boolean;

    export function phoneticsConvert(word: string): string;

    export function shouldConvert(word: string, mode?: string | null, use_ml?: boolean | null): boolean;

}
declare type InitInput = RequestInfo | URL | Response | BufferSource | WebAssembly.Module;

declare interface InitOutput {
    readonly memory: WebAssembly.Memory;
    readonly chatNormalize: (a: number, b: number) => [number, number];
    readonly convertText: (a: number, b: number, c: number, d: number, e: number, f: number) => [number, number];
    readonly convertWord: (a: number, b: number, c: number, d: number, e: number, f: number) => [number, number];
    readonly isForeignOrReject: (a: number, b: number) => number;
    readonly isUzbek: (a: number, b: number, c: number, d: number, e: number) => number;
    readonly phoneticsConvert: (a: number, b: number) => [number, number];
    readonly shouldConvert: (a: number, b: number, c: number, d: number, e: number) => number;
    readonly __wbindgen_externrefs: WebAssembly.Table;
    readonly __wbindgen_malloc: (a: number, b: number) => number;
    readonly __wbindgen_realloc: (a: number, b: number, c: number, d: number) => number;
    readonly __wbindgen_free: (a: number, b: number, c: number) => void;
    readonly __wbindgen_start: () => void;
}

declare type SyncInitInput = BufferSource | WebAssembly.Module;

declare namespace wasm_bindgen {
    /**
     * Instantiates the given `module`, which can either be bytes or
     * a precompiled `WebAssembly.Module`.
     *
     * @param {{ module: SyncInitInput }} module - Passing `SyncInitInput` directly is deprecated.
     *
     * @returns {InitOutput}
     */
    export function initSync(module: { module: SyncInitInput } | SyncInitInput): InitOutput;
}

/**
 * If `module_or_path` is {RequestInfo} or {URL}, makes a request and
 * for everything else, calls `WebAssembly.instantiate` directly.
 *
 * @param {{ module_or_path: InitInput | Promise<InitInput> }} module_or_path - Passing `InitInput` directly is deprecated.
 *
 * @returns {Promise<InitOutput>}
 */
declare function wasm_bindgen (module_or_path?: { module_or_path: InitInput | Promise<InitInput> } | InitInput | Promise<InitInput>): Promise<InitOutput>;
