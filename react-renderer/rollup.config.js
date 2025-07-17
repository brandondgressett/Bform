import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import typescript from '@rollup/plugin-typescript';
import dts from 'rollup-plugin-dts';
import { readFileSync } from 'fs';

const pkg = JSON.parse(readFileSync('./package.json', 'utf8'));

export default [
  {
    input: 'src/index.ts',
    output: [
      {
        file: pkg.main,
        format: 'cjs',
        sourcemap: true,
        exports: 'named'
      },
      {
        file: pkg.module,
        format: 'esm',
        sourcemap: true,
        exports: 'named'
      }
    ],
    plugins: [
      resolve({
        browser: true,
        preferBuiltins: false
      }),
      commonjs(),
      typescript({
        tsconfig: './tsconfig.json',
        declaration: false,
        declarationMap: false
      })
    ],
    external: [
      'react',
      'react-dom',
      'bootstrap',
      '@ag-grid-community/react',
      '@ag-grid-community/core',
      '@hookform/resolvers',
      '@rjsf/core',
      '@rjsf/utils',
      '@rjsf/validator-ajv8',
      '@rjsf/bootstrap-4',
      'react-hook-form',
      'yup',
      'classnames'
    ]
  },
  {
    input: 'dist/index.d.ts',
    output: [{ file: 'dist/index.d.ts', format: 'esm' }],
    plugins: [dts()],
    external: [/\.css$/]
  }
];